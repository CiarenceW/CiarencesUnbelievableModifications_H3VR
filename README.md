# Ciarence's Unbelievable Modifications
- The best (and only) Receiver 2 QoL mod, now for H3VR! 
### Collection of changes that were too small to release individually

## Changelog
`1.0.0`  
Initial release

`1.1.0`  
New Features:
 - Added a tweak to play the unused Handle Up lock sounds for guns with rotating charging handles (pretty much every one of them except the MP5s)
 - Added a tweak to hold a magazine in a reversed pose
  
Changes: 
 - Cylinder Bullet Collector:
	- Tried to make the ejector animate when ejecting

`1.1.1`  
Fixed typo in source link

`1.2.0`  
New Features:
 - Added a patch to keep the original rotation when you palm a magazine
 - Added a patch to make all gun stocks start out folded
 - Added a patch to prevent your other hand from snatching your currently holded gun
 - Added a patch to re-enable the Institution preview scene  
 
`1.3.0`  
New Features:  
 - Added a patch to grab shells in a competitive style
 - Added a patch to grab a singular shell by pressing trigger instead of grip
 - Added a patch to knock non palmable magazines out of AK pattern (or all guns without a magazine release, if the option is enabled)
 - Added a patch to blacklist specific guns or categories of guns from Easy Mag Loading and Virtual Stock  
 - Coloured console output!!!!!!
  
Changes: 
 - ReverseMagHold:
	- Now automatically disables when BetterHands' mag palming is enabled
 - KeepPalmedMagazineRotation:  
	- Fixed incompatibility with Melon's ammo boxes
 - Spawn Stock Tweaks:
	- Now automatically disables itself when a scenario loads (fixes issues with safehouses)

## Features

### Keep Palmed Magazine rotation
 - Keeps the original rotation difference when you palm a magazine
		
	![](https://cdn.discordapp.com/attachments/881715672214810638/1192885378286358619/MagPalmKeepOffset.gif?ex=663bb914&is=663a6794&hm=66f1328520286071086b5393a2618ff0cbc44ba0ed8816316c79bd11c6efcada&)

### Reverse Mag Grip  
 - Grab a magazine with your hand facing the floor to grab a magazine in a reverse grip
 
 - You can also reverse it in your hand by pressing the trigger (in Streamlined) or left/right (in Classic)
 
 - If you are in Classic, you can hold the trigger and push the touchpad up/down to adjust the position of the magazine
 
	![](https://cdn.discordapp.com/attachments/881715672214810638/1172205236438442005/ReverseMagGrip.gif?ex=663ba2b2&is=663a5132&hm=d26cff4ddd02b35bf8eb0c40dbd466ee439f9dba0092ca01ba028ba59b3f71c2&)

### Cylinder Bullet Collector
 - Eject and keep unspent casings from a revolver's cylinder by pressing trigger while grabbing it :)

	![](https://cdn.discordapp.com/attachments/881715672214810638/1166101634896646335/CylinderBulletCollectorShowcase.gif?ex=663bd7c6&is=663a8646&hm=71485cd242ca1a8019963d60b8b0c00316113fc6c2f7d6ca3e8aab87537b595b&)

### Mag Retention Tweaks
 - Quick Retained Mag Release:
	- Allows you to release a retained magazine by letting go of touchpad
	
	![](https://cdn.discordapp.com/attachments/881715672214810638/1166100711105380423/MagRetentionTweaks1Showcase.gif?ex=663bd6ea&is=663a856a&hm=efdc29e7a4a5940fff7a6647823536d8a09a7a58e64890f91623a318fc55f263&)
	
 - Angle & Distance Thresholds:
	- Adds configurable angle and distance thresholds to prevent a magazine from being retained, good for people who like having a magazine in their offhand while holding a handgun
	
	![](https://cdn.discordapp.com/attachments/881715672214810638/1166100710451060767/MagRetentionTweaks2Showcase.gif?ex=663bd6ea&is=663a856a&hm=c3e333c80e46c88954dc0386648ae22f2cbe70f076229a7fae91634c5fcc569f&)
	
### Gun Snatching Prevention
 - Prevents your gun from being snatched by your other hand
 
	![](https://cdn.discordapp.com/attachments/881715672214810638/1192885378751938670/GunSnatchPrevention.gif?ex=663bb915&is=663a6795&hm=c82da25e96afb0cc61297eaa877545b78bb899e79088255d73a62983e161cbf6&)
	
### Spawn Stock Tweaks
 - Folds all gun stocks when spawned
	
	![](https://cdn.discordapp.com/attachments/881715672214810638/1192885379414634598/SpawnStockTweaks.gif?ex=663bb915&is=663a6795&hm=fd37b5224a9ad87c780aab74c7b396ecf028128292c960ef941d791565116f10&)
	
### Competitive Shell Grabbing  
 - Competitive Shell Grabbing:  
	- Allows you to grab shotgun shells in a shotgun competition shooting style  
	  
	![](https://cdn.discordapp.com/attachments/881715672214810638/1237534129042624564/compettitiveshellgrabbingg.gif?ex=663bfef8&is=663aad78&hm=5a901e8646ed3e423ce1975df10243405837014a307cbab5ecf4d819bde9d582&)
	
	- Allows you to grab a single shotgun shell by pressing trigger
	
### Knock AK Mag Out  
 - Allows you to knock out a non palmable magazine from an AK pattern rifle (or any rifle with the option enabled) by pressing thumbstick down on the hand holding the gun (classic controls only)
  
	![](https://cdn.discordapp.com/attachments/881715672214810638/1237535059666276382/KnockAKMagOut.gif?ex=663bffd6&is=663aae56&hm=8d7808fb7eb379a0d03f40a40e5f1a53eef2ccd4d7f8d1c368a37ba106588727&)
	
### Option Gun Category Blacklister
 - Allows you to blacklist certain guns and categories of guns from Easy Mag Loading and Virtual Stock

## Credits timeeeeeeeeeee
- Szikaka for the Receiver 2 stuff and helping with the math bullshit in these patches  
- 42nfl19 for helping with the testing of recent patches
- Jackfoxtrot for the BoltHandleLockSoundTweaks suggestion
- ME
