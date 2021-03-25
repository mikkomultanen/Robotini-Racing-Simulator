linux-player:
	/Applications/2020.3.0f1/Unity.app/Contents/MacOS/Unity -quit -batchmode -buildLinux64Player robotini-linux.x86_64

webgl-player:
	/Applications/2020.3.0f1/Unity.app/Contents/MacOS/Unity -batchmode -buildTarget WebGL -executeMethod WebGLBuilder.build -logFile build.log