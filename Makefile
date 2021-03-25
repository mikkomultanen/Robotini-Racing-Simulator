linux-player:
	/Applications/2020.3.0f1/Unity.app/Contents/MacOS/Unity -quit -batchmode -buildLinux64Player robotini-linux.x86_64

webgl-player:
	/Applications/2020.3.0f1/Unity.app/Contents/MacOS/Unity -quit -nographics -batchmode -buildTarget WebGL -executeMethod WebGLBuilder.build -logFile build.log

publish-webgl-player:
	tar -cf robotini-web-player.tar robotini-web-player
	aws s3 cp robotini-web-player.tar s3://robotini-race-results/ --acl public-read