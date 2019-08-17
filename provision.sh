#!/bin/bash -ex

sudo apt-get update

# Adapted script from this blog entry: https://towardsdatascience.com/how-to-run-unity-on-amazon-cloud-or-without-monitor-3c10ce022639

sudo apt-get install -y gcc make linux-generic
sudo apt-get install -y xserver-xorg
sudo apt-get install -y mesa-utils
 
sudo echo 'blacklist nouveau'  | sudo tee -a /etc/modprobe.d/blacklist.conf
sudo echo 'options nouveau modeset=0' | sudo tee -a /etc/modprobe.d/blacklist.conf
sudo echo 'options nouveau modeset=0' | sudo tee -a /etc/modprobe.d/nouveau-kms.conf

sudo update-initramfs -u

wget http://us.download.nvidia.com/XFree86/Linux-x86_64/430.40/NVIDIA-Linux-x86_64-430.40.run
sudo /bin/bash NVIDIA-Linux-x86_64-430.40.run --accept-license --no-questions --ui=none

sudo nvidia-xconfig -a --use-display-device=None --virtual=1280x1024
sudo sed -i 's/    BoardName      "GRID K520"/    BoardName      "GRID K520"\n    BusID          "0:3:0"/g' /etc/X11/xorg.conf

tar -xvf robotini-runner.tar