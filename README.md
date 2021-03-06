# Model Colour Editor
## Features
- Convert material colours to vertex colours on import
- Override vertex colours on a per-mesh basis with a centralised editor window
- Extendable interface for generating colours for sets of meshes via custom Scriptable Objects

![](Media/screenshot.png)

## Installation
Requires Unity 2019.3 or later.

Install via one of the following options:
- Add the following Git URL to the Unity Package Manager:
  >```https://github.com/samcrisp/model-colour-editor.git#release```
- [Download the latest package](https://github.com/samcrisp/model-colour-editor/releases/latest) and install in either of the following ways:
  - Open the Package Manager and select "Import package from disk..." and select the .unitypackage file.
  - From the Assets menu select "Import Package > Custom Package..." and select the .unitypackage file. Note: Model Colour Editor requires the [Editor Coroutines](https://docs.unity3d.com/Packages/com.unity.editorcoroutines@1.0/manual/index.html) package to be installed as a dependency. Installing via the Assets menu will not resolve the dependency automatically. Follow the prompts in the Settings tab to install the Editor Coroutines package.

## Getting Started
Watch the tutorial video to get started: https://www.youtube.com/watch?v=M43X3IhDJoY

- Open the editor window from the menu "Window > Model Colour Editor".
- Select one or more meshes or models from the Project or Hierarchy windows.
- From the Model Colour Editor window you can:
  - Preview any vertex colours in the selection.
  - Add vertex colour overrides.
  - Enable importing material colours from a model as vertex colours.
  - Create and use Colour Picker Tools to generate various colours on multiple meshes.

Note: You will need a material which can display vertex colours in order to render them. Unity's default shaders don't use vertex colours out of the box, however Model Colour Editor provides some Materials that render vertex colours in the Examples folder of the package. If you have installed the package via the Package Manager, the example materials can be found in the Project directory under "Packages/Model Colour Editor/Examples/Materials".

## Acknowledgements
This plugin is supported by the Victorian Government through Creative Victoria.

![](Media/CreativeVictoriaLogo_lores.jpg)