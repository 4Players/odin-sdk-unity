# 4Players ODIN Unity SDK

ODIN is a cross-platform software development kit (SDK) that enables developers to integrate real-time chat technology into multiplayer games, apps and websites.

The ODIN package for Unity adds real-time VoIP communication to your game, thus making it more social and interactive, while increasing the immersion of the experience for your players.

[Online Documentation](https://docs.4players.io/voice/unity)

## Prerequisites

- Unity 2022.3 or any later version

This Unity version was chosen as it is Long Term Supported and provides compatibility with all targeted platforms.

**Note**: WebGL is currently not supported. Support for WebGL builds will return with the upcoming ODIN web SDK.

## Installation

The package can be installed in multiple ways.

### Unity Package

Please download the latest version of the ODIN Unity SDK as a `.unitypackage` from the [Github releases](https://github.com/4Players/odin-sdk-unity/releases) page. Just double-click the `.unitypackage` to import it into your current Unity editor project.

### Package Manager

Using the Package Manager will ensure that all dependencies are set up correctly and that you will have the most up to date version of the SDK. In most cases, using the Package Manager is the way to go.

To open the Package Manager, navigate to `Window` and then click `Package Manager` in your Unity Editor menu bar.

#### Using a Git Repository

Click the + button in the upper left and select `Add package from git URL`. Next, enter this URL and hit enter to import the package:

[https://github.com/4Players/odin-sdk-unity.git](https://github.com/4Players/odin-sdk-unity.git)

#### Using a Tarball Archive

Click the + button in the upper left and select `Add package from tarball`. Next, select the odin.tgz archive you've downloaded from the [Github releases](https://github.com/4Players/odin-sdk-unity/releases) page to import the package.

## Samples

We ship a sample package with the Unity SDK, which contains several examples and a demo scene. To import it into your project, open the Package Manager and hit import on the `Examples` package.

### Usage

The base Prefab Asset is available at `Packages/io.fourplayers.odin/Runtime/OdinInstance.prefab` and Script accessibility with the `OdinRoom` class.

## Troubleshooting

Contact us through the listed methods below to receive answers to your questions and learn more about ODIN.

### Discord

Join our [official Discord server](https://4np.de/discord) to chat with us directly and become a part of the 4Players ODIN community.

### Email

Don’t use Discord or X? Send us an [email](mailto:odin@4players.io) and we’ll get back to you as soon as possible.
