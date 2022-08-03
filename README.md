<p align="center"><a href="https://miguelmj.github.io/Candle"><img src="logo.svg" alt="logo" height="200px"/></a></p>
<h1 align="center">Candle.NET</h1>
<h3 align="center">2D lighting for SFML.NET</h3>
<p align="center">
<img src="https://img.shields.io/badge/-.NET%206-5C2D91?style=flat-square"/>
<img src="https://img.shields.io/badge/SFML.Net-v2.5.0-8CC445?logo=SFML&style=flat-square"/>
<a href="https://miguelmj.github.io/Candle"><img src="https://img.shields.io/badge/code-documented-success?style=flat-square"/></a>
<img src="https://img.shields.io/badge/version-v1.0-informational?style=flat-square"/>
<a href="LICENSE"><img src="https://img.shields.io/badge/license-MIT-informational?style=flat-square"/></a>
</p>
Candle.NET is a SFML based C# library that provides light, shadow casting and field of view functionalities with easy integration.
It is originally written in C++, and this project is its version for .Net languages.

There is no tutorial for Candle.NET, but you can use the C++ resources.

[Official documentation](https://miguelmj.github.io/Candle), [C++ version](https://github.com/MiguelMJ/Candle).

## Authors
* [Miguel Mejía Jiménez](https://github.com/MiguelMJ) - main developer
* [RD3V](https://github.com/RDevWasTaken) - .NET version developer

## Demo

Before anything, here you have a little example of how it looks.

<p align="center"><img src="https://github.com/MiguelMJ/Candle/raw/master/doc/img/demo.gif" height="400"></p>

The code comes with a demo program showing the functionalities provided by the library. In it you can place lights and edges that will cast shadows, and modify the behaviour of the fog.

You can check the full manual of the demo [here](https://miguelmj.github.io/Candle/demo_manual.html).

## Contributing

This project is more likely to only be a .Net version of the [existing library](https://github.com/MiguelMJ/Candle). Meaning, any functionality which is not in the original Candle won't be added here.

However, if you want to improve the code or add support for more .Net versions, then you are free to go.

## Example

With SFML.Net and Candle.NET installed you can run the following code:

```C#
using SFML.Graphics;
using SFML.System;
using SFML.Utils;
using SFML.Window;
using Candle;

namespace Demo
{
    public class Example
    {
        static void Main(string[] args)
        {
            // Create a window
            RenderWindow window = new RenderWindow(new VideoMode(400, 400), "app");
            window.Closed += (s, e) => window.Close();

            // Create a light source
            RadialLight light = new RadialLight()
            {
                Range = 150F
            };

            // Create an edge pool
            List<Line> edges = new List<Line>
            {
                new Line(new Vector2f(200F, 100F), new Vector2f(200F, 300F))
            };

            window.MouseMoved += (s, e) =>
            {
                light.Position = (Vector2f)Mouse.GetPosition(window);
                light.CastLight(edges);
            };

            // Main loop
            while (window.IsOpen)
            {
                window.DispatchEvents();

                window.Clear();
                window.Draw(light);
                window.Display();
            }
        }
    }
}
```

The result will be a simple light casting a shadow over an invisible wall in the center of the window.

<p align="center"><img src="https://github.com/MiguelMJ/Candle/raw/master/doc/img/example.gif" height="300"/></p>

## License
Candle uses the MIT license, a copy of which you can find [here](LICENSE), in the repo.

It uses the external library SFML, that is licensed under the zlib/png license.