using SFML.Graphics;
using SFML.System;
using SFML.Utils;
using SFML.Window;
using Candle;

namespace Demo
{
    public class App
    {
        /*
         * WINDOW
         */
        private const uint WIDTH = 700;
        private const uint HEIGHT = 700;
        private const uint MENU_W = HEIGHT / 8;
        private RenderWindow _window;
        private View _sandboxView, _menuView;

        /*
        * LIGHTING
        */
        private LightingArea _lighting;
        private bool _glow = true;
        private bool _persistent_fog = true;
        private List<LightSource> _lights1; // all
        private List<LightSource> _lights2; // glowing
        private List<Line> _edgePool;
        private VertexArray _edgeVertices;
        private Texture _fogTex;

        /*
         * BACKGROUND
         */
        private const int ROWS = 16;
        private const int COLS = 16;
        private float CELL_W = WIDTH / COLS;
        private float CELL_H = HEIGHT / ROWS;
        private VertexArray _background;

        /*
        * INTERACTIVITY - Brushes - Lights
        */
        public enum Brush
        {
            None = -1,
            Radial = 0,
            Directed = 1,
            Block = 2,
            Line = 3
        };
        private Brush _brush;
        private bool _lineStarted;

        private VertexArray _mouseBlock;
        private float _blockSize;

        private RadialLight _radialLight;
        private DirectedLight _directedLight;
        private bool _control, _shift, _alt;

        /*
        * INTERACTIVITY - Menu
        */
        class Button : Transformable, Drawable 
        {
            public static Color buttonA1 = new Color(50, 50, 250, 255);
            public static Color buttonA2 = new Color(40, 40, 40, 255);
            public static Color buttonZ1 = new Color(50, 50, 50, 255);
            public static Color buttonZ2 = new Color(20, 20, 20, 255);
            static int BC = 0;
            public RectangleShape rect;
            private Drawable icon;
            public Action<App> function;

            public Button(Drawable d, Action<App> f)
            {
                float bw = (MENU_W - 4);
                rect = new RectangleShape(new Vector2f(bw, bw))
                {
                    FillColor = buttonZ1,
                    OutlineColor = buttonZ2,
                    OutlineThickness = 2
                };
                Position = new Vector2f(2, 2 + (BC) * (bw + 4));
                icon = d;
                function = f;
                BC++;
            }

            ~Button()
            {

            }

            public bool Contains(Vector2f p) 
            {
                return Transform.TransformRect(rect.GetGlobalBounds()).Contains(p.X, p.Y);
            }

            public void Draw(RenderTarget target, RenderStates states) 
            {
                states.Transform = Transform;
                target.Draw(rect, states);

                if (icon != null)
                    target.Draw(icon, states);
            }
        }

        private List<Button> _buttons;

        private static int _color_i = 0;

        /*
        * SUBROUTINES
        */
        Vector2f GetMousePosition()
        {
            Vector2f mp = _window.MapPixelToCoords(Mouse.GetPosition(_window));

            if (_control)
            {
                mp = new Vector2f
                (
                    CELL_W * (0.5F + MathF.Round(mp.X / CELL_W - 0.5F)),
                    CELL_H * (0.5F + MathF.Round(mp.Y / CELL_H - 0.5F))
                );
            }
            return mp;
        }

        public App()
        {
            _lighting = new LightingArea(LightingArea.Mode.Fog, new Vector2f(), new Vector2u(WIDTH, HEIGHT));

            _window = new RenderWindow(new VideoMode(WIDTH + MENU_W, HEIGHT), "Candle - demo");
            _window.SetFramerateLimit(60);
            _window.Closed += (s, e) => _window.Close();
            _window.MouseMoved += (s, e) => UpdateOnMouseMove();
            _window.MouseWheelScrolled += (s, e) => UpdateOnMouseScroll((0 < e.Delta ? 1 : 0) - (e.Delta < 0 ? 1 : 0));
            _window.KeyPressed += (s, e) => UpdateOnPressKey(e.Code);
            _window.KeyReleased += (s, e) => UpdateOnReleaseKey(e.Code);
            _window.MouseButtonPressed += (s, e) =>
            {
                if (e.Button == Mouse.Button.Left)
                    Click();
                else
                    SetBrush(Brush.None);
            };
            _window.MouseButtonReleased += (s, e) =>
            {
                if (e.Button == Mouse.Button.Left)
                {
                    _lineStarted = false;
                    foreach (var b in _buttons)
                    {
                        b.rect.FillColor = Button.buttonZ1;
                        b.rect.OutlineColor = Button.buttonZ2;
                    }
                }
            };

            _lights1 = new List<LightSource>();
            _lights2 = new List<LightSource>();
            _edgePool = new List<Line>();

            float totalWidth = WIDTH + MENU_W;
            _edgeVertices = new VertexArray(PrimitiveType.Lines);
            _background = new VertexArray(PrimitiveType.Quads, ROWS * COLS * 4);

            try
            {
                _fogTex = new Texture("texture.png");

                _lighting.SetAreaTexture(_fogTex);
                _lighting.Scale = new Vector2f(WIDTH / _fogTex.Size.X, HEIGHT / _fogTex.Size.Y);
            }
            catch (Exception)
            {
                Console.WriteLine("No texture detected!");
                _lighting.AreaColor = Color.Black;
            }

            _lighting.Clear();
            _mouseBlock = new VertexArray(PrimitiveType.Lines, 8);
            _sandboxView = new View(new Vector2f(WIDTH / 2F, HEIGHT / 2F), new Vector2f(WIDTH, HEIGHT))
            {
                Viewport = new FloatRect(0F, 0F, WIDTH / totalWidth, 1F)
            };
            _menuView = new View(new Vector2f(MENU_W / 2F, HEIGHT / 2F), new Vector2f(MENU_W, HEIGHT))
            {
                Viewport = new FloatRect(WIDTH / totalWidth, 0F, MENU_W / totalWidth, 1F)
            };
            _radialLight = new RadialLight
            {
                Range = 100F
            };
            _directedLight = new DirectedLight()
            {
                Range = 200F,
                BeamWidth = 200F
            };

            Color[] BG_COLORS = {
                Color.Green,
                Color.Blue,
                Color.Red
            };

            int colors = BG_COLORS.Length;
            for (int i = 0; i < COLS * ROWS; i++)
            {
                uint p1 = (uint)(i * 4);
                uint p2 = p1 + 1;
                uint p3 = p1 + 2;
                uint p4 = p1 + 3;
                float x = CELL_W * (i % COLS);
                float y = CELL_H * (i / COLS);

                _background.ModifyVertex(p1, (ref Vertex v) => 
                {
                    v.Color = BG_COLORS[i % colors];
                    v.Position = new Vector2f(x, y);
                });

                _background.ModifyVertex(p2, (ref Vertex v) =>
                {
                    v.Color = BG_COLORS[i % colors];
                    v.Position = new Vector2f(x, y + CELL_H);
                });

                _background.ModifyVertex(p3, (ref Vertex v) =>
                {
                    v.Color = BG_COLORS[i % colors];
                    v.Position = new Vector2f(x + CELL_W, y + CELL_H);
                });

                _background.ModifyVertex(p4, (ref Vertex v) =>
                {
                    v.Color = BG_COLORS[i % colors];
                    v.Position = new Vector2f(x + CELL_W, y);
                });
            }

            SetMouseBlockSize(CELL_W);
            _brush = Brush.None;
            _lineStarted = false;
            _control = false;
            _shift = false;
            _alt = false;

            _buttons = new List<Button>();
            var i1 = new CircleShape(MENU_W / 3F, 60)
            {
                FillColor = new Color(255, 255, 150, 255),
                OutlineColor = Color.White,
                OutlineThickness = 2,
                Position = new Vector2f(MENU_W / 6F, MENU_W / 6F)
            };
            _buttons.Add(new Button(i1, app => app.SetBrush(Brush.Radial)));

            var i2 = new RectangleShape(new Vector2f(MENU_W * 2F / 3F, MENU_W / 2F))
            {
                FillColor = new Color(255, 255, 150, 255),
                OutlineColor = Color.White,
                OutlineThickness = 1,
                Position = new Vector2f(MENU_W / 6F, MENU_W / 4F)
            };
            _buttons.Add(new Button(i2, app => app.SetBrush(Brush.Directed)));

            var i3 = new RectangleShape(new Vector2f(MENU_W / 2F, MENU_W / 2F))
            {
                FillColor = new Color(25, 25, 25, 255),
                Position = new Vector2f(MENU_W / 4, MENU_W / 4)
            };
            _buttons.Add(new Button(i3, app => app.SetBrush(Brush.Block)));

            var i4 = new RectangleShape(new Vector2f(MENU_W, 3F))
            {
                Position = new Vector2f(MENU_W * 2F / 15F, MENU_W * 2F / 15F),
                Rotation = 45F,
                FillColor = new Color(25, 25, 25, 255)
            };
            _buttons.Add(new Button(i4, app => app.SetBrush(Brush.Line)));

            var i7 = new VertexArray(PrimitiveType.Quads, 12);
            for (uint i = 0; i < 12; i++)
            {
                i7.ModifyVertex(i, (ref Vertex v) => v = _background[i]);
            }
            i7.Transform(new Transform
            (
                MENU_W * 2F / 7F / CELL_W, 0F, MENU_W / 14F - 1F,
                0F, MENU_W * 2F / 7F / CELL_H, MENU_W * 5F / 14F - 1F,
                0F, 0F, 1F
            ));
            i7.Darken(0.2F);
            _buttons.Add(new Button(i7, app => app._lighting.Opacity = Clamp(app._lighting.Opacity - 0.1F)));

            var i8 = new VertexArray(i7);
            i8.Darken(0.5F);
            _buttons.Add(new Button(i8, app => app._lighting.Opacity = Clamp(app._lighting.Opacity + 0.1F)));

            var i5 = new VertexArray(PrimitiveType.Quads);
            i5.Append(new Vertex(new Vector2f(1, 0)));
            i5.Append(new Vertex(new Vector2f(2, 0)));
            i5.Append(new Vertex(new Vector2f(2, 3)));
            i5.Append(new Vertex(new Vector2f(1, 3)));
            i5.Append(new Vertex(new Vector2f(0, 1)));
            i5.Append(new Vertex(new Vector2f(3, 1)));
            i5.Append(new Vertex(new Vector2f(3, 2)));
            i5.Append(new Vertex(new Vector2f(0, 2)));
            i5.SetColor(new Color(255, 255, 150, 255));

            float a = MathF.PI / 4;
            float s = MENU_W * 2 / 9;
            i5.Transform(new Transform
            (
                s * MathF.Cos(a), s * -MathF.Sin(a), MENU_W / 2F,
                s * MathF.Sin(a), s * MathF.Cos(a), 0F,
                0F, 0F, 1F
            ));
            _buttons.Add(new Button(i5, app => app.ClearLights()));

            var i6 = new VertexArray(i5);
            i6.SetColor(new Color(25, 25, 25, 255));
            _buttons.Add(new Button(i6, app => { app.ClearEdges(); app.CastAllLights(); }));
        }

        private static float Clamp(float x)
        {
            return MathF.Max(0F, MathF.Min(1F, x));
        }

        void capture()
        {
            Texture tex = new Texture(_window.Size.X, _window.Size.Y);
            tex.Update(_window);

            RenderTexture rt = new RenderTexture(WIDTH, HEIGHT);
            rt.SetView(new View(new Vector2f(WIDTH / 2, HEIGHT / 2), new Vector2f(WIDTH, HEIGHT)));
            rt.Draw(new Sprite(tex));
            rt.Display();

            string name = $"candle-capture-{DateTime.Now.ToString("yyyy.MM.dd_HH.mm.ss")}.png";

            if (!rt.Texture.CopyToImage().SaveToFile(name))
                Environment.Exit(1);
        }
        
        void SetMouseBlockSize(float size)
        {
            if (size > 0)
            {
                _mouseBlock.ModifyVertex(7, (ref Vertex v) => v.Position = new Vector2f(-size / 2F, -size / 2F));
                _mouseBlock.ModifyVertex(0, (ref Vertex v) => v.Position = new Vector2f(-size / 2F, -size / 2F));
                _mouseBlock.ModifyVertex(1, (ref Vertex v) => v.Position = new Vector2f(size / 2F, -size / 2F));
                _mouseBlock.ModifyVertex(2, (ref Vertex v) => v.Position = new Vector2f(size / 2F, -size / 2F));
                _mouseBlock.ModifyVertex(3, (ref Vertex v) => v.Position = new Vector2f(size / 2F, size / 2F));
                _mouseBlock.ModifyVertex(4, (ref Vertex v) => v.Position = new Vector2f(size / 2F, size / 2F));
                _mouseBlock.ModifyVertex(5, (ref Vertex v) => v.Position = new Vector2f(-size / 2F, size / 2F));
                _mouseBlock.ModifyVertex(6, (ref Vertex v) => v.Position = new Vector2f(-size / 2F, size / 2F));

                _blockSize = size;
                if (_brush == Brush.Block)
                {
                    PopBlock();
                    PushBlock(GetMousePosition());
                    CastAllLights();
                }
            }
        }

        void PushEdge(Line edge)
        {
            _edgePool.Add(edge);
            _edgeVertices.Append(new Vertex(edge.Origin));
            _edgeVertices.Append(new Vertex(edge.Point(1F)));
        }

        void PopEdge()
        {
            _edgePool.RemoveAt(_edgePool.Count - 1);
            _edgeVertices.Resize(_edgeVertices.VertexCount - 2);
        }

        void PushBlock(Vector2f pos)
        {
            Vector2f[] points = {
                pos + _mouseBlock[0].Position,
                pos + _mouseBlock[2].Position,
                pos + _mouseBlock[4].Position,
                pos + _mouseBlock[6].Position,
            };

            Polygon p = new Polygon(points);

            foreach (var l in p.GetLines())
                PushEdge(l);
        }
            
        void PopBlock()
        {
            for (int i = 0; i < 4; i++)
            {
                PopEdge();
            }
        }

        void DrawBrush()
        {
            Transform t = new Transform();
            t.Translate(GetMousePosition());
            switch (_brush)
            {
                case Brush.Line:
                    _window.Draw(new CircleShape(1.5f), new RenderStates(t));
                    break;
                default:
                    break;
            }
        }

        public void SetBrush(Brush b)
        {
            if (b != _brush)
            {
                if (b == Brush.Block)
                    PushBlock(GetMousePosition());

                if (_brush == Brush.Block)
                {
                    PopBlock();
                    CastAllLights();
                }

                if (_lineStarted)
                {
                    PopEdge();
                    CastAllLights();
                    _lineStarted = false;
                }

                _brush = b;
                UpdateOnMouseMove();
            }
        }

        public void CastAllLights()
        {
            foreach (var l in _lights1)
                l.CastLight(_edgePool);
        }

        public void Click()
        {
            Vector2f mp = GetMousePosition();

            if (mp.X > WIDTH)
            {
                mp.X -= WIDTH;
                bool press = false;
                foreach (var button in _buttons)
                {
                    if (button.Contains(mp))
                    {
                        button.rect.FillColor = Button.buttonA1;
                        button.rect.OutlineColor = Button.buttonA2;
                        button.function(this);
                        press = true;
                    }
                    else
                    {
                        button.rect.FillColor = Button.buttonZ1;
                        button.rect.OutlineColor = Button.buttonZ2;
                    }
                }

                if (!press) 
                    SetBrush(Brush.None);
            } 
            else
            {
                switch (_brush)
                {
                    case Brush.Radial:
                        {
                            LightSource nl = new RadialLight(_radialLight);
                            _lights1.Add(nl);

                            if (_glow)
                                _lights2.Add(nl);
                        }
                        break;
                    case Brush.Directed:
                        {
                            LightSource nl = new DirectedLight(_directedLight);
                            _lights1.Add(nl);

                            if (_glow)
                                _lights2.Add(nl);
                        }
                        break;
                    case Brush.Line:
                        PushEdge(new Line(mp, 0F));
                        _lineStarted = true;
                        break;
                    case Brush.Block:
                        PushBlock(mp);
                        break;
                    default:
                        break;
                }
            }
        }
        public void UpdateOnMouseMove()
        {
            Vector2f mp = GetMousePosition();
            if (mp.X < WIDTH)
            {
                switch (_brush)
                {
                    case Brush.Block:
                        PopBlock();
                        PushBlock(mp);
                        CastAllLights();
                        break;
                    case Brush.Radial:
                        _radialLight.Position = mp;
                        _radialLight.CastLight(_edgePool);
                        break;
                    case Brush.Directed:
                        _directedLight.Position = mp;
                        _directedLight.CastLight(_edgePool);
                        break;
                    case Brush.Line:
                        if (_lineStarted)
                        {
                            Vector2f orig = _edgePool[_edgePool.Count - 1].Origin;
                            PopEdge();
                            PushEdge(new Line(orig, mp));
                            CastAllLights();
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        public void UpdateOnMouseScroll(int d)
        {
            if (GetMousePosition().X < WIDTH)
            {
                switch (_brush)
                {
                    case Brush.Radial:
                        if (_alt)
                            _radialLight.Rotation += d * 6F;
                        else if (_shift)
                            _radialLight.BeamAngle = _radialLight.BeamAngle + d * 5F;
                        else
                            _radialLight.Range = MathF.Max(0F, _radialLight.Range + d * 10F);

                        _radialLight.CastLight(_edgePool);
                        break;
                    case Brush.Directed:
                        if (_alt)
                            _directedLight.Rotation += 6F * d;
                        else if (_shift)
                            _directedLight.BeamWidth = _directedLight.BeamWidth + d * 5F;
                        else
                            _directedLight.Range = MathF.Max(0F, _directedLight.Range + d * 10F);

                        _directedLight.CastLight(_edgePool);
                        break;
                    case Brush.Block:
                        SetMouseBlockSize(_blockSize + d * CELL_W);
                        break;
                    default:
                        break;
                }
            }
        }

        void UpdateOnPressKey(Keyboard.Key k)
        {
            switch (k)
            {
                case Keyboard.Key.M:
                    {
                        bool textured = _lighting.AreaTexture != null;
                        if (_lighting.AreaMode == LightingArea.Mode.Fog)
                        {
                            _lighting.AreaMode = LightingArea.Mode.Ambient;
                            _lighting.AreaColor = textured ? Color.White : Color.Yellow;
                        }
                        else
                        {
                            _lighting.AreaMode = LightingArea.Mode.Fog;
                            _lighting.AreaColor = textured ? Color.White : Color.Black;
                        }
                    }
                    break;
                case Keyboard.Key.T:
                    _persistent_fog = !_persistent_fog;
                    break;
                case Keyboard.Key.P:
                    capture();
                    break;
                case Keyboard.Key.Q:
                case Keyboard.Key.Escape:
                    _window.Close();
                    break;
                case Keyboard.Key.LControl:
                    _control = true;
                    break;
                case Keyboard.Key.LAlt:
                    _alt = true;
                    break;
                case Keyboard.Key.LShift:
                    _shift = true;
                    break;
                case Keyboard.Key.R:
                    SetBrush(Brush.Radial);
                    break;
                case Keyboard.Key.D:
                    SetBrush(Brush.Directed);
                    break;
                case Keyboard.Key.B:
                    SetBrush(Brush.Block);
                    break;
                case Keyboard.Key.L:
                    SetBrush(Brush.Line);
                    break;
                case Keyboard.Key.A:
                    _lighting.Opacity = Clamp(_lighting.Opacity + 0.1F);
                    break;
                case Keyboard.Key.Z:
                    _lighting.Opacity = Clamp(_lighting.Opacity - 0.1F);
                    break;
                case Keyboard.Key.S:
                    if (_brush == Brush.Radial || _brush == Brush.Directed)
                    {
                        _radialLight.Intensity = Clamp(_radialLight.Intensity + 0.1F);
                        _directedLight.Intensity = Clamp(_directedLight.Intensity + 0.1F);
                    }
                    break;
                case Keyboard.Key.X:
                    if (_brush == Brush.Radial || _brush == Brush.Directed)
                    {
                        _radialLight.Intensity = Clamp(_radialLight.Intensity - 0.1F);
                        _directedLight.Intensity = Clamp(_directedLight.Intensity - 0.1F);
                    }
                    break;
                case Keyboard.Key.G:
                    if (_brush == Brush.Radial || _brush == Brush.Directed)
                    {
                        _glow = !_glow;
                    }
                    break;
                case Keyboard.Key.F:
                    if (_brush == Brush.Radial || _brush == Brush.Directed)
                    {
                        _radialLight.Fade = !_radialLight.Fade;
                        _directedLight.Fade = !_directedLight.Fade;
                    }
                    break;
                case Keyboard.Key.C:
                    if (_brush == Brush.Radial || _brush == Brush.Directed)
                    {
                        Color[] L_COLORS = {
                            Color.White,
                            Color.Magenta,
                            Color.Cyan,
                            Color.Yellow
                        };

                        int n = L_COLORS.Length;
                        _color_i = (_color_i + 1) % n;
                        _radialLight.Color = L_COLORS[_color_i];
                        _directedLight.Color = L_COLORS[_color_i];
                    }
                    break;
                case Keyboard.Key.Space:
                    _lineStarted = false;
                    if (_alt)
                        ClearEdges();
                    else if (_shift)
                        ClearLights();
                    else
                        ClearAll();
                    CastAllLights();
                    break;
                default:
                    break;
                }
            }

        void UpdateOnReleaseKey(Keyboard.Key k)
        {
            switch (k)
            {
                case Keyboard.Key.LControl:
                    _control = false;
                    break;
                case Keyboard.Key.LAlt:
                    _alt = false;
                    break;
                case Keyboard.Key.LShift:
                    _shift = false;
                    break;
                default:
                    break;
            }
        }

        public void ClearLights()
        {
            _lights1.Clear();
            _lights2.Clear();
        }

        public void ClearEdges()
        {
            _edgePool.Clear();
            _edgeVertices.Clear();
            _lineStarted = false;

            if (_brush == Brush.Block) 
                PushBlock(GetMousePosition());
        }

        public void ClearAll()
        {
            ClearLights();
            ClearEdges();
        }

        public void MainLoop()
        {
            Clock clock = new Clock();

            while (_window.IsOpen)
            {
                _window.DispatchEvents();

                if (_persistent_fog)
                    _lighting.Clear();

                foreach (var l in _lights1)
                    _lighting.Draw(l);

                if (_brush == Brush.Radial)
                    _lighting.Draw(_radialLight);
                else if (_brush == Brush.Directed)
                    _lighting.Draw(_directedLight);

                _lighting.Display();

                _window.Clear();

                _window.SetView(_menuView);
                foreach (var b in _buttons)
                    _window.Draw(b);

                _window.SetView(_sandboxView);
                _window.Draw(_background);
                _window.Draw(_lighting);

                foreach (var l in _lights2)
                    _window.Draw(l);

                if (_glow)
                {
                    if (_brush == Brush.Radial)
                        _window.Draw(_radialLight);
                    else if (_brush == Brush.Directed)
                        _window.Draw(_directedLight);
                }

                _window.Draw(_edgeVertices);
                DrawBrush();

                _window.Display();

                Time dt = clock.Restart();
                int fps = (int)MathF.Abs(1F / dt.AsSeconds());
                _window.SetTitle($"Candle demo [{fps} fps: {dt.AsMilliseconds()} ms] " +
                    $"({_lights1.Count + ((_brush == Brush.Radial || _brush == Brush.Directed) ? 1 : 0)} Light/s  " +
                    $"{_edgePool.Count} Edge/s)");
            }
        }
    }

    public class Program
    {
        static void Main(string[] args)
        {
            new App().MainLoop();
        }
    }
}