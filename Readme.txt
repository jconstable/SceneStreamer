M3 Engineering Test
=============

The Unity project includes a set of scripts located in Extensions/SceneLoader. This SceneLoader 
feature allows for the streaming of additively-loaded Scenes, which is very important not only 
for game performance, but also for collaboration on large teams. 

The test should be return within one week’s time. Plan to spend a reasonable amount of time on 
your improvements, and prioritize quality over quantity.

Please comment on any problems you see, even if you don't end up addressing them. Feel free to ask 
questions about project and Scene setup.


Project Setup:

* We encourage you to install a version of Unity 2020.1 from https://tinyurl.com/y6h9f6l6
* Open Assets/Scenes/Track.unity
* Hit play, and observe that Scenes are dynamically loaded and unloaded to form the current parts 
  of the track.
* In the Track scene, you will see a few instances of the SceneLoader
  - "Debug - LoadAllScenes" instance of SceneLoader allows you to load a whole set of streamed scenes,
    such that a content creator can work on an entire level.
  - Other GameObjects contain SceneLoader instances, along with trigger volumes that control when 
    Scenes are streamed at runtime.
* Scenes are streamed using Addressables


Requirements:

Create an Editor window that manages SceneLoaders for a given master Scene (in this case, Track.unity is 
a master Scene). For possible inspiration, reference other Level Streaming features online.
The Editor window should support the following features:
* List existing SceneLoaders
* Indicate what trigger volumes load which Scenes
* Allow for the possibility of adding a new/existing Scene to the Master Scene (i.e., the tool will create all required GameObjects 
  and components for loading an additional Scene).

Your new feature should be as user-friendly as possible, with the audience being level designers and 
artists. Feel free to change anything in the project that you like: code, GameObject structure, level
layout, project structure, etc.

Return project submissions by creating a branch of this repository, and sharing your new branch's URL.

Supply a summary of the work you performed along with your new zip file. Items to mention
should include:
* Modifications made to the project
* Limitations of your implementation
* (Optional) Future improvements you might make to the feature, were time not an issue
* (Optional) Whatever thoughts you had about this test!


Bonus Point Issues:
- Currently, you must use the inspector on each SceneLoader to change behavior. Could all this be (optionally) 
  controlled from your new window?
- Is there a way to build a node-based visualization of the Master Scene?
- Could/should the trigger volumes be part of the SceneLoader itself, instead of being a separate object?
- There is currently no visualization for the trigger volumes that trigger SceneLoad actions, other 
  than the default BoxCollider gizmos.
- Currently, Scenes need to have contents positioned properly in world space. Maybe the loader could 
  use an offset to position loaded contents?
