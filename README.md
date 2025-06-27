Discord: smookyz2024

This autopotion solution aims to even the playing field for all WoE players allowing even the 300+ ping players to pot similar or EVEN FASTER than 50 ping players when the current hp drops.
The bottleneck is the default item usage delay of 100ms. This means there is no reason to purchase other autopotion programs as they will simply not provide any meaningful gain when your ping is 50 or greater. You can still use it if your ping is below 50. Just pause the program using the pause key. The pause key only disables the constant hp key spam. It will end up behaving like a reguar autopotter

I put all the code into the program.cs file to make it easier for newer or beginner programmers to ctrl+f and edit what they need to add. I kept it singlethreaded as multithreading provides no advantage here.

Explanation -> https://www.youtube.com/watch?v=P06RzfgapOg

SmookyzAP in action -> https://www.youtube.com/watch?v=ecfqVSuzZu8

As you can see, I can survive even comas with no devotion while being on top of enemy meteor spam. I maintain almost 500fps even while using the program.

Instructions:

Remember, you need a huge supply of HP potions so this is intended for Midrate or WoE-Competitive Servers.
This potter will always spam the HP Key. Watch the Explanation video please.
You also need a grf with the proper stateicons + tgas for the buffs. Default data.grf has it all except the box of sunlight but I am sure woe players by now have it all.
There is a "Pause" hotkey that you can configure in the config.ini file. This will pause the Permanent HP key press but it does not pause the entire program. That means it will behave as a regular autopotter while paused.
Edit the autobuffdelay as your ping allows (also in the config.ini)

You can edit the memory address in the config.ini and also the window title to make it work for other servers. You only need the current HP address. 
