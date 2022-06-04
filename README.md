# Capstone_Design_2022

## Abstract
우리는 임베디드 AI 컴퓨팅 플랫폼로부터 나온 자세 추정 데이터를 이용하여 게인엔진인 Unity를 사용하여 멀티 네트워킹(Photon Unity Networking)상황에서 캐릭터가 사용자의 움직임을 
따라할 수 있게 함으로써 사람 자세 추정을 이용한 Metaverse 시스템의 구현을 목표로 한다.

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
