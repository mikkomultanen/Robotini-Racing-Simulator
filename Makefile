build-ami:
	tar -cf robotini-runner.tar robotini-linux.x86_64 robotini-linux_Data
	packer build packer.json
