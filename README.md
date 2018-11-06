# Unity Selection History

Have you ever wished you could go to your previous selection in Unity without having to manually click it again?

Now you can! 

![Menu](images/selection_history_menu.png)

In this demo I'm going backwards and forwards in my selection history using the side buttons on my mouse:

![Demo](images/demo.gif)

## Features

* Navigate backwards and forwards in your selection history (like undo/redo).
* Remembers selections of assets, folders, scene objects, etc. (anything that can show up in the Selection.objects API).
* History is preserved across script reloads and enter/exit of playmode.
* Smart enough to ignore selections of objects that no longer exist (or that were in a scene that is no longer loaded).

## Installation

Just clone/download this repo somewhere into your project and it should Just Work.

## Mouse Back/Forward Buttons

I have not found a way to use the side buttons on a mouse for the history navigation in Unity yet. My hope is the release of the new shortcut manager will make this easy to accomplish. On Windows I am using AutoHotkey as a workaround. The included UnityInspectorHistory.ahk script can be used to add mouse navigation support.

## Compatibility

I've tested this in Unity 2018.2+, but it should work from 2017.4 and later. Earlier versions aren't supported because I depend upon the AssemblyReloadEvents class to prevent the loss of history during script reloads.
