# Avisos de terceros / Third-party notices

El código de **Minecraft Desktop Hotbar** se distribuye bajo GPL-3.0 (ver `LICENSE`).
Este proyecto también incluye o usa los siguientes componentes de terceros, cada uno con su propia licencia.

---

## Minecraft (Mojang Studios / Microsoft)

Las texturas de interfaz (hotbar, inventario, corazones, comida, barra de experiencia, ítems), los sonidos (`.ogg`), el aspecto del personaje y la estética visual de Minecraft son propiedad de **Mojang Studios / Microsoft**.

- **No** están cubiertos por la licencia GPL-3.0 de este proyecto.
- Se incluyen únicamente como parte de un **proyecto de fans, gratuito y sin fines comerciales**.
- Este proyecto **no es oficial** y no está aprobado ni asociado con Mojang ni Microsoft.
- "Minecraft" es una marca registrada de Mojang Synergies AB.

Si eres titular de alguno de estos derechos y quieres que se retire algún archivo, abre un *issue* en el repositorio y se retirará.

---

## skinview3d

Usado para el modelo 3D del personaje en el inventario (`app/assets/skinview3d.bundle.js`).
https://github.com/bs-community/skinview3d

```
MIT License

Copyright (c) 2014-2018 Kent Rasmussen
Copyright (c) 2017-2022 Haowei Wen, Sean Boult and contributors

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

---

## three.js

Incluido dentro de `skinview3d.bundle.js` (motor 3D WebGL).
https://github.com/mrdoob/three.js

```
The MIT License

Copyright © 2010-2026 three.js authors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
```

---

## Microsoft Edge WebView2

`Microsoft.Web.WebView2` (SDK) y el runtime de WebView2 son de Microsoft, distribuidos bajo sus propios términos de licencia:
https://www.nuget.org/packages/Microsoft.Web.WebView2

## .NET

La versión portátil incluye el runtime de .NET 8 (MIT, © .NET Foundation and Contributors):
https://github.com/dotnet/runtime/blob/main/LICENSE.TXT
