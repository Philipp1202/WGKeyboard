# Word-Gesture Keyboard
This package includes a Word-Gesture Keyboard for writing

## functions
- Writing words/letters with gestures (by moving the controller inside the keyboard's hitbox and pressing the trigger button)
- Adding new words via the options button "add word"
- Changing the layout of the keyboard (new layouts can be added in the Assets/layouts.txt file)

## important prefab and setup
You need to drag the (name TBD) prefab into your scene. Then you need to connect your function to write words somewhere with the "name TBD"/WGKeyboard/WGKMain.cs script to the "result" variable.
You then should be able to write with the Word-Gesture Keyboard.


## implemented approach
The system that has been implemented was more or less SHARK2. But it does not use any language information nor dynamic channel weighting by gestureing speed. 
The paper can be found here (TODO: add link to paper)


## classes
TODO: LIST ALL CLASSES + SHORT TEXT ABOUT WHAT THEY DO