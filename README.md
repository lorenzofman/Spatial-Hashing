This demo is an exercise for my Advanced Computer Graphics class.

Its purpose is to remove trees that intersect with a road (represented by a cubic Bézier curve). The number of trees must be large (> 1M) and the application must run in real-time. The Bézier curve should be split in 10K segments. To overcome these challenges, a spatial hashing solution was used to limit the number of distance calculations required per segment

I decided to use Unity DOTS to handle a great number of entities instead of writing it in C++/Vulkan because of the large amount of Unity tools of and its easy workflow. 

The linear memory Spatial hash algorithm was based on:

> Pozzer, Cesar & De, Cícero & Pahins, Cícero & Heldal, Ilona & Mellin, Jonas & Gustavsson, Per. (2014). A Hash Table Construction Algorithm for Spatial Hashing Based on Linear Memory. 2014. 10.1145/2663806.2663862. 
