ContactShadows
==============

![gif](https://i.imgur.com/02oI7jL.gif)
![gif](https://i.imgur.com/sSaakib.gif)

This is an experimental implementation of contact shadows in Unity.

**Contact Shadows** is used to fill gaps between objects and shadows that are
caused by shadow bias. It uses the screen-space ray tracing technique to
determine regions of shadows more precisely than shadow mapping. It also
employs the temporal reprojection filter technique to reduce processing load
and artifacts caused by ray tracing.

System requirements
-------------------

- Unity 2017.2 or later

How to Use
----------

The `ContactShadows` component only renders shadows about a specific pair of
a light and a camera. So you have to specify which pair of light and camera is
going to be rendered with the component.

1. Add the `ContactShadows` component to a camera.
2. Set a light to the `Light` property of the component.

There are three parameters in the component:

**Rejection Depth** - This defines the depth of each pixels, which controls
the thickness of shadows. It's recommended to set this value to the average
thickness of the objects in the view.

**Sample Count** - The number of samples used per ray.

**Temporal Filter** - This controls the strength of the temporal reprojection
filter. You can get smoother results by increasing this value, but it also
introduces gaps between objects and shadows when moving the camera. It's
recommended to keep the value lower for fast-moving cameras. The filter is to
be disabled when this value is set to zero.

Current Limitations
-------------------

- **At the moment, Contact Shadows only supports directional lights.**
- Contact Shadow is still under optimization work. It can be quite slow in
  some situations. Average GPU time is 1.5ms - 2.0ms under FHD (1080p) on
  Radeon RX 480.

License
-------

Copyright (c) 2017 Unity Technologies

This repository is to be treated as an example content of Unity; you can use
the code freely in your projects. Also see the [FAQ] about example contents.

[FAQ]: https://unity3d.com/unity/faq#faq-37863
