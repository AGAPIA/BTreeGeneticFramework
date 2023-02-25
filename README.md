# Automatic diffculty management and testing in games using a framework based on behavior trees and genetic algorithms

This project addresses the problem of using behavior trees for automatic testing and diffculty tuning in games.
It is based on our paper: https://arxiv.org/abs/1909.04368
The main contribution of this work is to report and sketch a framework implementation that uses behavior trees and genetic algorithms at its core, which could provide the following features to the game development industry:
  - Automatic diffculty management in games. Diffculty can also be adapted per user to offer the right level of challenge for each level.
  - Create more diversity by generating new behaviors with less human effort.
  - Automatically identify possible exploits that users can use in their advantage.
  - A way to automatize more the functional testing of the gameplay component

The mentioned features were evaluated using a 3D game environment used for prototyping and quick results evaluation created by us and made open source for the community.

The connection architecture between a game application and our framework looks like this:

![image](https://user-images.githubusercontent.com/26081910/221363534-baa25f64-2a52-4a26-8586-9d4cc1c81de0.png)

For more details and explanations of the architecture, see our paper.
