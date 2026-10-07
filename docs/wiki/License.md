# License

RavenTween is open source under the **MIT License**. You can use it in personal and commercial projects, modify it and redistribute it, as long as the copyright notice and license text are kept with the source.

```
MIT License

Copyright (c) 2026 Nekuzaky

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```

## Third-party content

The editor glyphs on the Play / Stop / Complete buttons and in the Monitor window come from [Bootstrap Icons](https://github.com/twbs/icons), also under the MIT License. The full notice ships with the package in `Third Party Notices.md`. The runtime contains no third-party code.

## What RavenTween adds to your project

| Path | What it is |
| :--- | :--- |
| `Packages/com.raventween.core` | The package, managed by the Package Manager. Don't edit it in place. |
| Assemblies `RavenTween.Runtime`, `RavenTween.Editor`, `RavenTween.TextMeshPro` | Separate assembly definitions, so RavenTween never leaks into your own assemblies. The TextMeshPro one compiles only when TMP is present. |

RavenTween writes no settings asset and nothing into your `Assets` folder. Imported samples go to `Assets/Samples/RavenTween/` and can be deleted freely.

## Support

Bugs and feature requests: [GitHub Issues](https://github.com/Nekuzaky/RavenTween/issues). Other questions: [contact@nekuzaky.com](mailto:contact@nekuzaky.com).

You can support the project on [GitHub Sponsors](https://github.com/sponsors/nekuzaky) or [Buy Me a Coffee](https://buymeacoffee.com/nekuzaky).
