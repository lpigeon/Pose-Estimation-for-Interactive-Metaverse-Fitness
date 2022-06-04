# Capstone_Design_2022

## Abstract
Virtual reality for fitness and health care applications require accurate and real-time pose estimation for interactive features. Yet, they suffer either a limited angle of view when using handset devices such as smartphones and VR gears for capturing human pose or a limited input interfaces when using distant imaging/computing devices such as Kinect. Our goal is to marry these two into an interactive metaverse system with human pose estimation. This paper describes the design and implementation of Yoroke, a distributed system designed specifically for human pose estimation for interactive metaverse system. It consists of a remote imaging device for estimating human pose, and a handset device for implementing a multi-user interactive metaverse system. We have implemented and deployed Yoroke on embedded platforms and evaluated its effectiveness in delivering accurate and real-time pose estimation for multi-user interactive metaverse platform.

![image](https://user-images.githubusercontent.com/69760395/171997257-49de6007-4758-4d6a-be0d-593a37dcfeef.png)


## Notion
* https://www.notion.so/5f85d14ce67749229d304be4cbdce208

## Development Environment
* Ubuntu 18.04
* python-opencv 3.4.10
* Tensorflow 1.14.0
* Tensorflow-GPU
* Cuda 11.2
* Cudnn 8.1.0
* Unity

## Version
* v1 : Photon2와 unitychan_dynamic_locomotion을 이용한 기본적인 네트워크와 공간 구성
* v2 : 2명 기준 pose estimation
* v3 : 3명 이상 pose estimation 및 로비, 캐릭터 선정 가능
* v4 : UI upgrade
* v5 : Unity상에서 delay 없는 pose estimation을 위해서 데이터를 읽는 방식 변경
* yoroke : 최종 배포

## Reference
* https://github.com/SrikanthVelpuri/tf-pose
* https://github.com/Jacob12138xieyuan/real-time-3d-pose-estimation-with-Unity3D
* https://openaccess.thecvf.com/content_cvpr_2017/papers/Chen_3D_Human_Pose_CVPR_2017_paper.pdf
* https://junsk1016.github.io/deeplearning/tf-pose-estimation-%EB%B9%8C%EB%93%9C/
* https://www.youtube.com/watch?v=mXPndbtKbTo
