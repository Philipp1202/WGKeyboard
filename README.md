# Word-Gesture Keyboard
This package includes a Word-Gesture Keyboard for writing

## functions
- Writing words/letters with gestures (by moving the controller inside the keyboard's hitbox and pressing the trigger button)
- Adding new words via the options button "add word"
- Changing the layout of the keyboard (how to add new layouts can be found under *how to create and use new layouts*)

## important prefab and setup
You need to drag the (name TBD) prefab into your scene. Then you need to connect your function to write words somewhere with the "name TBD"/WGKeyboard/WGKMain.cs script to the "result" variable.
You then should be able to write with the Word-Gesture Keyboard.

## how to create and use new layouts
In the Assets folder you will find a file named "layouts.txt". The first six lines will be ignored by the program and give you some information about what is allowed and what is not allowed.
To create a new layout follow the instructions in the layouts.txt file.
After you created a new layout, you need to execute the "word_sokgraph_generator.py" script in the Assets/Additional_Scripts folder. To do so, navigate to the Assets/Additional_Scripts folder with your terminal. Then type "python3 word_sokgraph_generator.py *layout* *wordlist*". *layout* is the name of the layout you want to be able to use afterwards. *wordlist* is the name of the wordlist you want to generate graphs for (it must be in the Assets folder (if something seems not to work with special character, make sure your wordlist is an utf-8 textfile)).
After executing it, you have to wait some seconds, then it should have generated/overwritten a file for the chosen layout. Now you can start the program and should be able to write the words in the textfile with gestures (if all the characters for a certain word are available in the layout).


## implemented approach
The system that has been implemented is a weaker version of the SHARK2 system. It does not use any language information nor dynamic channel weighting by using the gesturing speed. 
The paper can be found [here](https://www.researchgate.net/publication/228875756_SHARK2A_large_Vocabulary_shorthand_writing_system_for_pen-based_computers).


## classes
TODO: LIST ALL CLASSES + SHORT TEXT ABOUT WHAT THEY DO
WGKMain.cs is the main file. It has instances of the other important classes and looks for the userinput.
FileHanlder.cs is for the handling of files, e.g. reading the layouts file, but also writing new words/ graphpoints into the wordlist/graphfiles, when a new word is being added.
GraphPointsCalculator.cs is for the calculations considering the graphs needed for SHARK2.
KeyboardHelper.cs is to create the keyboard visually and stores some attributes of the keyboard, like the keyradius or the keyboards length.
USerInputHandler.cs manages the userinput, but only regarding to the gestures. Calculates the points, the user intented to write with his gesture.