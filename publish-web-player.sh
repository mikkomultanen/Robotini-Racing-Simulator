tar -cf robotini-web-player.tar robotini-web-player
aws s3 cp robotini-web-player.tar s3://robotini-race-results/ --acl public-read