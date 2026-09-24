# Audio Visualization

## What is it?
A simple audio visualization based on momentary audio frequency from ~2017-2019.


## State
Technical features: Pretty much finished.
Content: Open end. Three levels are finished. I can easily come back to create more content if I ever want to.


## Goal
This projected started as a 'side quest' when in another project I wanted to make some wall move depending on some song that is playing right now. I soon realized that I could do all kinds of fancy stuff with the audio visualization so I created a new project dedicated for it. The goal was to just have fun implementing the audio visualization and come up with some cool scenarios. It also soon became a nice way of expressing my creativity.


## Concept
After creating a dedicated project for the audio visualization, I played around with vertext transformation based on the values of 8 frequency channels utilizing the default Unity audio sampler. A flat area in which the vertices of the plane would move up and change their color depending on the current value of its channel. Lower frequencies apply to the left vertices, higher frequencies apply to the right vertices.<br><br>

But I soon realized, that I could do all kinds of things with the output of the audio sampler. In the end, the sampler gives me back any amount of plain float values depending on the amount of channels you choose. I realized that with these information I could change properties in the wold such as the strength or radius of light sources, the position, scale and rotation of objects or values of particle systems, I could trigger events when certain channels reach certain vaules or even change the game speed depending on the bass/bpm.<br><br>

Thus I started working on a way to easily and without having to write code change the properties of objects in the world. I implemented something similar to, but as I quickly learned clearly not as good as, visual scripting. I implemented generalized scripts pipeline that can build a supply chain to pull the float value from the audio sampler of any frequency channel (e.g. from the bass channel), transform this value with mathematical operations (e.g. scripts for addition, multiplication, min/max values, lerping etc), transform it into different types if needed (e.g. into colors or animation curves) and finally give this information into an applicator script which applies the float/color/animationCurve/whatever value into the affected components field (such as the radius of a light source, the scale of a transform, the velocity of particles of a particle system or anything else).<br><br>

All of these scripts have to be implemented once for each mathematical operation, type transformation and component and then you could just mix and match these scripts to quickly apply audio based changes to objects.


## Learnings

### How audio sampling works
I had no previous knowledge about how audio sampling works before starting this project, nor how audio information is stored in files. I now know the gist of it. But I will be honest with you, this is so long ago that I have probably forgot most of it by now. Still a very intersting topic!

### Namespaces
I knew about namespaces from college and when reading foreign code, but as someone who at that time hasn't done big projects in which namespaces were neccesssary, I never really used them. I tried to wrap my head around them and tried to give almost all scripts their own namespace hierarchy. It was a mistake that I learned *a lot* from. I ended up with abominations such as TODO. After this fiasko, I ended up using namespaces sparsely in private projects and I primarily use it as a way to keep things ordered in my head. It helps a lot in challenging yourself when it comes to the question 'what namespace does the script I am about to create actually belong to?' and it makes you second guess the purpose of the script, which can lead to more purposeful decisions.

### 'Visual' scripting pipeline
I liked my idea of being able to put together a pipeline of scripts that allow me to add a few scripts together to quickly change the properties of game objects based on the audio that is being played right now. And the system DOES work it can be used. But one of the main problems was that in my mind I would always only have to pass information to the next script in a one-way manner, but I noticed that TODO
