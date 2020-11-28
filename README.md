# Unity-MarchingCubes-VertexSharing-GPU
THIS DOES NOT WORK AT IT'S CURRENT STATE

This is the result of my countless hours of trying to implement runtime mesh generation on the gpu with vertex sharing based on this paper:
https://www.diva-portal.org/smash/get/diva2:846354/FULLTEXT01.pdf

I've stopped working on this a couple months ago after spending too much time debugging and trying to figure out why nothing works. I've tried everything that comes to mind

I'm uploading this here in case someone smarter than me sees this and wants to salvage my work. Theoretically everything should work as inteneded but due to
my limited knowledge on GPGPU programming I have not been able to draw anything using this method. I tried to follow the paper as precisely as I could.

The important scripts are:

ComputeInstance.cs (Responsible for setting everything up and making a commandbuffer to dispatch the computeshaders)

CaseCompute.compute (Figures out the marching cubes case number)

VertexCreationShader.compute (Creates vertices and saves their indices to a 3d texture for the next step)

VertexSharingShader.compute (Creates the final indices based on the last step)

Recommended unity version 2019.3 or above. And the rendering pipeline is URP because I wanted to make a custom renderer feature to render the terrain.

The code is terrible, not documented at all and makes no sense, but here it is.
