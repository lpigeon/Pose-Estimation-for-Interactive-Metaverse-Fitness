# Pose Estimation for Interactive Metaverse Fitness(Capstone_Design_2022)

## Abstract
Virtual reality for fitness and health care applications require accurate and real-time pose estimation for interactive features. Yet, they suffer either a limited angle of view when using handset devices such as smartphones and VR gears for capturing human pose or a limited input interfaces when using distant imaging/computing devices such as Kinect. Our goal is to marry these two into an interactive metaverse system with human pose estimation. This paper describes the design and implementation of Yoroke, a distributed system designed specifically for human pose estimation for interactive metaverse system. It consists of a remote imaging device for estimating human pose, and a handset device for implementing a multi-user interactive metaverse system. We have implemented and deployed Yoroke on embedded platforms and evaluated its effectiveness in delivering accurate and real-time pose estimation for multi-user interactive metaverse platform.
<p align="center"><img src="https://user-images.githubusercontent.com/69760395/171997257-49de6007-4758-4d6a-be0d-593a37dcfeef.png"></p>

## Development Environment
* Ubuntu 18.04
* python-opencv 3.4.10
* Tensorflow 1.14.0
* Tensorflow-GPU
* Cuda 11.2
* Cudnn 8.1.0
* Unity

## Version
* v1: Basic network and spatial configuration using Photon2 and unitychan_dynamic_locomotion
* v2: pose estimation based on 2 people
* v3: Pose estimation of 3 or more people, lobby, and character selection possible
* v4 : UI upgrade
* v5: Change the way data is read for pose estimation without delay on Unity
* yoroke: Final deployment

## Reference
* https://github.com/SrikanthVelpuri/tf-pose
* https://github.com/Jacob12138xieyuan/real-time-3d-pose-estimation-with-Unity3D
* https://openaccess.thecvf.com/content_cvpr_2017/papers/Chen_3D_Human_Pose_CVPR_2017_paper.pdf
* https://junsk1016.github.io/deeplearning/tf-pose-estimation-%EB%B9%8C%EB%93%9C/
* https://www.youtube.com/watch?v=mXPndbtKbTo
