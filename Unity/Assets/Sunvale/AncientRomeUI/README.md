# Ancient Rome UI
**Created by Sunvale**

Welcome to the Ancient Rome UI pack. Thank you for your purchase! 

This package is a UI asset pack first and foremost, not a game framework. While we have included a substantial codebase—featuring advanced mechanics like procedural mesh generation and a fully draggable RPG inventory—this code is provided purely as a **bonus** to bring the demo scenes to life. The scripts are designed to showcase the visual capabilities of the UI rather than serve as a robust, scalable game architecture. As such, our support covers the UI/art assets, but we do not provide programming tutorials or support for the underlying C# game logic.

While this pack is designed to help you prototype your UI quickly with ready-made components and a large set of placeholder icons and visuals, building UI is a complex process. We highly recommend using the demo scenes to familiarize yourself with how everything is assembled. 

There are quite a few shaders and materials involved, which are all customizable and tweakable. **Make sure to duplicate materials before you start modifying them.** Otherwise, the pack may lose its original visual fidelity due to persistent changes. (If this happens, you can always re-import the package to fix it).

## Getting Started

The best way to understand the package is to explore the included demo scenes. We highly recommend starting here:

1. Navigate to `Assets/Sunvale/AncientRomeUI/Demos/Scenes/`
2. Open the **DemoHub** scene and hit Play. 
3. This hub scene lets you easily navigate between the Strategy, RPG, and Options demos so you can see all the UI components in action, complete with hover effects and animations. 
4. The hierarchy contains plenty of Canvas examples that you can enable and disable outside of Play Mode to see how they are structured.

## Requirements

- **Unity Version:** Requires Unity 2021.3 LTS or higher.
- **Render Pipeline Compatibility:** Fully compatible with the Built-in Render Pipeline, Universal Render Pipeline (URP), and High Definition Render Pipeline (HDRP).
- **TextMesh Pro (TMP):** This pack heavily utilizes TextMesh Pro for high-quality font rendering. Please ensure TextMesh Pro is installed in your project via the Package Manager.

## Buttons and Clickable Elements

- The pack utilizes lightweight, custom tweening and sound managers. Buttons work with sounds right out of the box with no additional setup required—as long as your scene has an Audio Listener (typically on the Main Camera).

## Practical Tips

- The pack includes Assembly Definition files so it won't slow down your project's compile times.
- Textures are tagged with Unity labels to help you search faster.
- To keep your project size manageable, you may want to remove unused textures and icons, importing only the specific assets you need.
- Before creating a production build, remember to create texture atlases or utilize Unity's Sprite Atlas system to reduce draw calls if necessary. **However, be mindful of custom shaders.** Shaders that sample specific textures—such as framed portraits, skill-shield button icons, or RPG items—may break if those textures are packed into an atlas. Be sure to exclude these specific textures when setting up your atlases.

## Mesh Generation

There are four instances where procedural mesh generation was used: the lines between skill/tech tree nodes, the full pie chart, the half-donut chart (the government/parliament chart), and the demographics line graph. 

While the mesh generation and controller scripts allow for basic customization, they were primarily designed for the demo scene to illustrate how graphs and charts can visually integrate with the rest of the UI. They are not intended to be robust, generalized tools covering the plethora of use cases that can arise during full game development.

## Folder Structure

- **Sunvale/Common/**: Contains shared resources (like basic scripts, generic shaders, and simple textures) that will be used across future Sunvale packs. You won't usually need to modify anything here.
- **Sunvale/AncientRomeUI/Demos/**: Contains example scenes, scripts, and demo-specific textures. You can safely delete this folder before shipping your game to save space.
- **Sunvale/AncientRomeUI/Documentation/**: Contains helpful manifests, including `ClassDescriptions.txt` and `ShaderDescriptions.txt`, which detail the purpose of every script and shader included.
- **Sunvale/AncientRomeUI/Prefabs/**: Ready-to-use, fully assembled UI elements like buttons, windows, tooltips, and pie charts. Drag these straight into your Canvas!
- **Sunvale/AncientRomeUI/Textures/**: All the raw art assets, categorized neatly.
- **Sunvale/AncientRomeUI/Runtime/**: All the C# scripts that power the buttons, animations, graph generators, and inventory logic.

## Troubleshooting

- **Buttons not animating under a Mask:** If a button is not working or animating properly, it might be because it is under a `Mask` component. This is a classic Unity uGUI issue. If possible, use a `RectMask2D` component instead of a standard `Mask`. If that is not possible, you may need to refactor the button's tweening logic to use `someImage.materialForRendering.SetFloat(...)` instead of `someImage.material.SetFloat(...)`.
- **Visible seams on marble textures:** Some marble textures are not perfectly tiling, which can cause visible seams. If this causes issues, use the `UIGlobalTextureTiling` script and adjust the scale and offsets to fix it.
- **UI looks dark or strange in HDRP:** In HDRP, the demo can appear darker or look unusual due to HDRP post-processing. To fix this, change your Canvas Render Mode from *Screen Space - Camera* to *Screen Space - Overlay*. This prevents post-processing from interfering with the UI.

## Fonts

The fonts included are from Google Fonts. We do not own them, but they are distributed under the Open Font License (OFL), allowing you to freely publish any games that use them.

## Sounds

The pack includes a small selection of sounds to make the UI feel more alive. These are all CC0 (public domain) sounds sourced from OpenGameArt and similar sites. You are free to use them, modify them, and include them in your projects without restriction.

## Support

Thank you for purchasing Ancient Rome UI! If you encounter any bugs, please reach out to us at sunvaleui@gmail.com.