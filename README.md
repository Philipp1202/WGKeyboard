# Word-Gesture Keyboard
This package includes a Word-Gesture Keyboard that can be used in VR.

## Main Functions
- [Writing with gestures](#how-to-write-words-and-characters)
- [Adding new words](#how-to-add-new-words)
- [Changing the layout of the keyboard](#how-to-create-and-use-new-layouts)

## Setup
If you have successfully installed the package, then you need to drag the "WGKeyboardObject" prefab into your scene (it can be found under Packages/Word-Gesture Keyboard/Assets/Prefabs). 
### Usage in vitrivr-VR
Since the package uses the Grabable.cs and the EventInteractable.cs scripts, which are from vitrivr-VR, these do not have to be added. The only thing that needs to be added are the two functions to write and delete words. When clicking on the WGKeyboardObject/WgKeyboard you can see the WGKMain.cs script. There are two functions to add under "Result (String)" and "Delete Event ()". In the "Result (String)" you have to click the "+" button drag and drop the "QueryController" from the ModularInputScene into the field. Then click on the arrow of the field where "no function" is written and choose SceneTextInputController and then "InputWord". Do the same for "Delete Event ()" but choose the "DeleteWord" function instead of "InputWord".

### Usage outside of vitrivr-VR
You need your own script that allows you to drag objects but also to interact with them (by clicking the trigger button of the VR controller) and hover them (touching the object with the VR controller (no buttons pressed)). If you need inspiration, look for the vitrivr-VR project on GitHub and search for the Grabable.cs and EventInteractable.cs scripts. You will also need a script that has functions to give the keyboard the ability to write into text fields and delete from them.
The script to grab objects needs to be attached to the "WGKeyboardObject" object. It is used to move the keyboard around when grabbing it inside its hitbox. The other script that allows you to hover objects and interact with them (it can also be the same script) needs to be attached to the following objects with the following functions:
|Object|Hover/Interact|Function|
|------|--------------|--------|
|WGKeyboardObject|Interact|WGKMain.DrawWord|
|WGKeyboardObject|Hover|WGKMain.HoverKeyboard|
|WGKeyboardObject.Add|Interact|WGKMain.AddNewWord|
|WGKeyboardObject.Options|Interact|WGKMain.EnterOptions|
|WGKeyboardObject.OptionObjects.Option1|Interact|WGKMain.ChangeSize|
|WGKeyboardObject.OptionObjects.Option2|Interact|WGKMain.EnterLayoutChoose|
|WGKeyboardObject.OptionObjects.Option3|Interact|WGKMain.EnterAddWordMode|
|WGKeyboardObject.OptionObjects.Scale.Plus|Interact|WGKMain.ScalePlus|
|WGKeyboardObject.OptionObjects.Scale.Minus|Interact|WGKMain.ScaleMinus|
|WGKeyboardObject.ChooseWord.Word1|Hover|BestWordChooseManager.ChooseWord|
|WGKeyboardObject.ChooseWord.Word2|Hover|BestWordChooseManager.ChooseWord|
|WGKeyboardObject.ChooseWord.Word3|Hover|BestWordChooseManager.ChooseWord|
|WGKeyboardObject.ChooseWord.Word4|Hover|BestWordChooseManager.ChooseWord|

|Prefab|Hover/Interact|Function|
|------|--------------|--------|
|Key|Hover|KeyManager.IsHovered|
|LayoutKey|Interact|LayoutKeyScript.ChooseLayout|

The WGKMain.cs script can be accessed, if you click on the "+" button (to add the function to hover/interaction) and drag and drop the WGKeyboard object there. For the other scripts: They are attached to the objects that need them.

If you did this, there is one last step. When clicking on the WGKeyboardObject/WgKeyboard you can see the WGKMain.cs script. There are two functions to add under "Result (String)" and "Delete Event ()". In the "Result (String)" you have to add a function that allows you to write words (written with the keyboard) into a text field or something like that. In the "Delete Event ()" you have to add a function that allows you to delete characters (whole words or single characters).

## How to write words and characters
To write words, the controller has to be in the keyboard's hitbox. Whether it is in it, can be seen from the color of the keyboard. If it is in, the color of the keyboard is white otherwise, it is gray. If it is in the hitbox, you can press the trigger button of the controller. Now, instead of tapping on every single character you want to input, you can make a gesture to input a whole word. Start pressing the trigger button when you are at the key displaying the first character of the word you want to input. Keep pressing the trigger button and move to the next character of the word and so on until you are at the last one, then release it. Now there are three possible outcomes. The first one is that the word you intended to write was written. In the second case, another word is written, but your intended word can be found on one of the buttons that hover slightly above the keyboard. To choose one of these buttons, simply move your controller to it and when it touches the button, the word just written gets swapped with the one of the button you just touched. In the third case, either no word or a wrong one is written and you cannot find your intended word in a button hovering slightly above the keyboard. Then you either have to delete the written word that is wrong by clicking on the backspace button once or try to write the word again.

## How to add new words
To add words to the lexicon you can simply click on the options button marked with a black gear. Then there appears a button with "add word" written on it. Click on this one to enter the "add word mode". Now you have to write the word by clicking on the keys on the keyboard (inputting single characters) as it was a conventional keyboard. If you wrote the whole word, click on the button on the lower right side of the keyboard where "add word to dict" is written. The word you just wrote will disappear. Now, you can click on the "add word" button again to leave the "add word mode". The newly added word can now be written as gesture.  
Some remarks: First, the word to be added cannot contain a space, therefore nothing will happen when you click on the spacebar while being in the "add word mode". Secondly, the word will be permanently added to the lexicon and the graph files of the other layouts, so that the word can also be written with other layouts without using the "word_graph_generator.py" file again (mentioned in the next section).

## How to create and use new layouts
In the assets folder, you will find a file named "layouts.txt". The first six lines will be ignored by the program and give you some information about what is allowed and what is not.
To create a new layout follow the instructions in the layouts.txt file.
After you created a new layout according to the rules, you need to execute the "word_graph_generator.py" script in the "Assets/Additional_Scripts" folder. To do so, navigate to the "Assets/Additional_Scripts" folder with your terminal. Then type:  
"python3 word_graph_generator.py ***layout*** ***wordlist***"  
***layout*** is the name of the layout you want to be able to use afterwards  
***wordlist*** is the name of the wordlist text file whose words you want to be able to write with the keyboard. (it must be in the assets folder (if something seems not to work with special characters, make sure your wordlist is an utf-8 text file)). Note that every word with uppercase characters will not be looked at because all words need to be written in lowercase.  
After executing it, you have to wait some seconds, then it should have generated/overwritten a file for the chosen layout. Now you can start the program and should be able to write the words in the text file with gestures (if not all the characters of a certain word are available in the layout, this certain word will have been skipped and hence cannot be written with the keyboard if the newly created layout is active).

## How to change the layout
To change the layout click on the options button marked with a black gear. Then a button with "change layout" written on it appears. If you press it, you will see a list of choosable layouts. If you click on one of these, the keyboard will change its layout immediately. 

## Features
- The screen on top of the keyboard always shows (while writing) the word that would be written if the user releases the trigger of the controller.
- After writing a word, the user can choose from up to four other words. Words that also have a high probability to be the intended word the user wanted to write are shown on top of the screen mentioned in the feature before. To choose one of these, the user has to touch the corresponding button with their controller.
- After writing a word, the whole word can be deleted by pressing the backspace button only once.
- After writing a word a space is put automatically. After writing single characters, a space is only put automatically if the last input was a word and not a single character.

## Implemented approach
The approach that has been implemented is a simplified version of the algorithm used in the SHARK<sup>2</sup> system. It is a simplified version because it does not use any language information or dynamic channel weighting by using the gesturing speed.  
The paper explaining SHARK<sup>2</sup> can be found [here](https://www.researchgate.net/publication/228875756_SHARK2A_large_Vocabulary_shorthand_writing_system_for_pen-based_computers).

## Classes
**WGKMain.cs** is the main file. It has instances of the other important classes and maintains the main update function. Additionally, it contains most of the functions that are used for the buttons (like the options button or its sub buttons) to check whether they are clicked on.  
**FileHandler.cs** is for the handling of files, e.g. reading the layouts file or graph files. It also writes new words/graph points into the word list/graph files, when a new word is being added.  
**GraphPointsCalculator.cs** is for the calculations also performed in the algorithm used in SHARK<sup>2</sup>. Mainly it is to compare the user input with the words in the lexicon and to determine which word has the highest probability to be the one, the user intended to write.  
**KeyboardHelper.cs** is to create the keyboard visually and store some attributes of the keyboard, like the radius of a key or the keyboard's length.  
**UserInputHandler.cs** manages the user input, but only regarding their gestures. It calculates where the user writes points on the keyboard and then transforms these so that they can be used by the GraphPointsCalculator.  
**LayoutKeyScript.cs**, **BestWordChooseManager.cs** and **KeyManager.cs** are used for the layout keys, the keys that show the best words the user can choose from and the keyboard keys.