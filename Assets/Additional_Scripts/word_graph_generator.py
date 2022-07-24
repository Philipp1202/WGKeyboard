import matplotlib.pyplot as plt
import math
import numpy as np
import time
import sys
import pathlib
import os

class wordGraphGenerator:
    def __init__(self, layout):
        self.layout = layout
        self.keyboardLength = 1
        self.layouts = []
        self.layoutKeys = {}
        self.availableChars = []
        self.readLayoutsFile()
        if layout not in self.layouts:
            print("Layout (", layout, ") not found. Maybe you wrote the layout's name wrong.")
            exit()

        #if layout == "qwertz":
        #    self.keyboardSet.append("1234567890_-")
        #    self.keyboardSet.append("qwertzuiopü")
        #    self.keyboardSet.append("asdfghjklöä")
        #    self.keyboardSet.append("yxcvbnm")
        #    self.keyboardSet.append(" ")
        #elif layout == "qwerty":
        #    self.keyboardSet.append("1234567890_-")
        #    self.keyboardSet.append("qwertyuiopü")
        #    self.keyboardSet.append("asdfghjklöä")
        #    self.keyboardSet.append("zxcvbnm")
        #    self.keyboardSet.append(" ")
        #else:
        #    pass # make other layouts

        #self.testWords = ["test", "hello", "bye", "legendary", "official", "dark", "bright", "computer", "school", "university"]
        
        for i in range(0, len(self.layoutKeys[self.layout][0])):
            lineLength = len(self.layoutKeys[self.layout][1][i]) + np.abs(self.layoutKeys[self.layout][0][i])
            if " " in self.layoutKeys[self.layout][1][i]:
                lineLength += 7 # because length of spacebar is 8 * normal keysize, that means 7 * keysize extra
            if "<" in self.layoutKeys[self.layout][1][i]:
                lineLength += 1 # because length of backspace is 2 * normal keysize, that means 1 * keysize extra
            if lineLength > self.keyboardLength:
                self.keyboardLength = lineLength

    def getCharacterPositions(self):
        letterPos = {}
        for y in range(0, len(self.layoutKeys[self.layout][0])):
            x = self.layoutKeys[self.layout][0][len(self.layoutKeys[self.layout][0]) - 1 - y]

            for letter in self.layoutKeys[self.layout][1][len(self.layoutKeys[self.layout][0]) - 1 - y]:
                if letter == " ":
                    x += 8
                    continue
                if letter == "<":
                    x += 2
                    continue
                #print(letter, " : ", y)
                letterPos[letter.lower()] = np.array([x, y])
                x += 1
        print("LETTERPOS: ", letterPos)
        return letterPos
        
    # returns a list of points for the given word (points where the "pressed" letters lie)
    def getPointsForWord(self, word, letterPos):
        points = []
        for letter in word.lower():
            points.append(letterPos.get(letter))    
        return points
        #letterPos = {}
        #for y in range(0, len(self.layoutKeys[self.layout][0])):
        #    x = 0
        #    if y == 3: # for the offset the different layers of the keyboard have
        #        x = 0.5
        #    elif y == 2:
        #        x = 0.75
        #    elif y == 1:
        #        x = 1.25

        #    if len(self.keyboardSet) != 5:    # no numbers in top row (doesn't need the first shift to the right, can be reversed)
        #        x -= 0.5

        #    for letter in self.layoutKeys[self.layout][1][len(self.keyboardSet) - 1 - y]:
        #        letterPos[letter] = np.array([x, y])
        #        x += 1

        #points = []
        #for letter in word:
        #    points.append(letterPos.get(letter))    
        #return points


    # returns summed up length for all distances between all given points
    def getLengthByPoints(self, pointsArr):
        dist = 0
        for i in range(0, len(pointsArr) - 1):
            distVec = pointsArr[i] - pointsArr[i+1]
            dist += math.sqrt(distVec[0]**2 + distVec[1]**2)
        return dist


    # returns the sampled points with a gap of "steps" (walking the graph of a word) for the given string (word)
    # word: string
    # steps: int 
    def getWordGraphStepPoint(self, word, steps, letterPos):
        for letter in word:
            if letter not in self.availableChars:
                return None, None
        letterPoints = self.getPointsForWord(word, letterPos)
        #letterPoints = normalize(letterPoints, 2) # for testing include this line
        length = self.getLengthByPoints(letterPoints)
        if length == 0:
            stepPoints = []
            currPos = letterPoints[0]
            for i in range(0, steps):
                stepPoints.append((currPos + np.array([0.5,0.5]))/self.keyboardLength)
            stepPointsNormalized = self.normalize(stepPoints, 1)
            return stepPoints, stepPointsNormalized
        
        stepSize = length / (steps - 1)
        distVecs = []
        for i in range(0, len(letterPoints)-1):
            distVecs.append(letterPoints[i+1] - letterPoints[i])
            
        numSteps = 1
        currStep = stepSize
        currPos = letterPoints[0]
        currPosNum = 0
        currDistVecNum = 0
        
        stepPoints = []
        
        stepPoints.append((currPos + np.array([0.5,0.5]))/self.keyboardLength)
        
        while numSteps < steps:
            distVec = distVecs[currDistVecNum]
            distVecLength = math.sqrt(distVec[0]**2 + distVec[1]**2) # much faster than using np.linalg.norm()
            if currStep != stepSize:
                #print(distVecLength, " and ", currStep)
                if distVecLength - currStep > -0.00001: # error for abandoned and acknowledged was here
                    numSteps += 1
                    currPos = currPos + distVec / distVecLength * currStep
                    distVecs[currDistVecNum] = letterPoints[currPosNum + 1] - currPos # calculate new distance vector
                    stepPoints.append((currPos + np.array([0.5,0.5]))/self.keyboardLength)
                    #print(currPos)
                    currStep = stepSize
                else:
                    currStep -= distVecLength
                    currDistVecNum += 1
                    currPosNum += 1
                    currPos = letterPoints[currPosNum]
                    
            elif int(distVecLength / stepSize + 0.00001) > 0: # adding 0.00001 to avoid rounding errors
                #print(distVecLength, " and ", stepSize)
                numPointsOnLine = int(distVecLength / stepSize + 0.00001)
                numSteps += numPointsOnLine
                for i in range(0, numPointsOnLine):
                    stepPoints.append(((currPos + (i+1) * (distVec / distVecLength * stepSize)) + np.array([0.5,0.5]))/self.keyboardLength)
                    #print(currPos + (i+1) * (distVec / distVecLength * stepSize))
                
                if distVecLength - numPointsOnLine * stepSize > 0.00001:
                    currStep -= (distVecLength - numPointsOnLine * stepSize)
                    
                currDistVecNum += 1
                currPosNum += 1
                currPos = letterPoints[currPosNum]
                    
            else:
                currStep -= distVecLength
                currDistVecNum += 1
                currPosNum += 1
                currPos = letterPoints[currPosNum]
            #print(currDistVecNum)
            
        stepPointsNormalized = self.normalize(stepPoints, 2)
        return stepPoints, stepPointsNormalized


    # normalizes the points according to the paper talking about SHARK2 (make all bounding boxes of shapes euqally big and
    # put the center to the (0,0) point)
    # letterpoints: np.array list, points to normalize
    # length: int, length the longest side of the boundingbox will have
    def normalize(self, letterPoints, length):
        (x,y) = self.getXY(letterPoints)
        
        boundingBox = [min(x), max(x), min(y), max(y)]
        boundingBoxSize = [max(x) - min(x), max(y) - min(y)]
        
        if max(boundingBoxSize[0], boundingBoxSize[1]) != 0:
            s = length / max(boundingBoxSize[0], boundingBoxSize[1])
        else:
            s = 1
        
        middlePoint = np.array([(boundingBox[0] + boundingBox[1]) / 2, (boundingBox[2] + boundingBox[3]) / 2])
        
        newPoints = []
        for point in letterPoints:
            newPoints.append((point - middlePoint) * s)
            
        return newPoints

    # functions below are only for showing the resulting plotted
    def getXY(self, wordPoints):
        xPoints = []
        yPoints = []
        for i in range(0, len(wordPoints)):
            xPoints.append(wordPoints[i][0])
            yPoints.append(wordPoints[i][1])
        return (xPoints, yPoints)

    def plotWordGraph(self, points):
        plt.figure(figsize = [10,3])
        plt.plot(points[0], points[1], 'ro-')
        plt.axis([-0.1, 9.1, -0.1, 2.1])
        
    def plotWordGraphSteps(self, points):
        plt.figure(figsize = [10,3])
        plt.plot(points[0], points[1], 'ro')
        plt.axis([-0.1, 9.1, -0.1, 2.1])
        
    def plotWordGraphStepsNormalized(self, points):
        plt.figure(figsize = [10,3])
        plt.plot(points[0], points[1], 'ro')
        plt.axis([-2.1, 2.1, -2.1, 2.1])

    def readLayoutsFile(self):
        path = os.path.dirname(pathlib.Path().resolve()) + "/layouts.txt"
        l = ""
        padding = []
        keys = []
        
        with open(path, "r", encoding="utf-8") as f:
            i = 0
            for line in f:
                if i < 6:
                    i += 1
                    continue
                if line == None:
                    break
                elif l == "":
                    l = line.rstrip()
                    self.layouts.append(l)
                    continue
                elif line.rstrip() == "-----":
                    self.layoutKeys[l] = (padding, keys)
                    padding = []
                    keys = []
                    l = ""
                    continue
                splits = line.split("$$")
                f = 0
                try:
                    padding.append(float(splits[1]))
                except:
                    padding.append(0)
                keys.append(splits[0])
                # print(splits[0] + ", " + splits[1])
            print("available layouts: ", self.layouts)

        availableChars = ""
        for i in range(0, len(self.layoutKeys[self.layout][1])):
            for character in self.layoutKeys[self.layout][1][i]:
                if character != " " and character != "<":
                    availableChars += character.lower()
        self.availableChars = availableChars
        print(self.availableChars)


def main():
    
    # print(pathlib.Path().resolve())
    # print(os.path.dirname(pathlib.Path().resolve()))
    layout = sys.argv[1]
    lexiconFilePath = os.path.dirname(pathlib.Path().resolve()) + "/" + sys.argv[2]   # sys.argv[2] has to be the name of the txt file, that lies in the Assets folder (the word lexicon)
    lexicon = []
    with open(lexiconFilePath, "r", encoding = "utf-8") as f:
        for line in f:
            lexicon.append(line.rstrip())
    print(lexicon)

    wsg = wordGraphGenerator(layout)

    f = open(lexiconFilePath, "a", encoding="utf-8")  # add all characters from the keyboard layout tp the lexicon, such that the user will be able to use these characters later on
    for char in wsg.availableChars:
        if char not in lexicon:
            lexicon.append(char)
            f.write(char)
            f.write("\n")
    f.close()

    start_time = time.time()
    try:
        f = open(os.path.dirname(pathlib.Path().resolve()) + "/Graph_Files/graph_" + layout + ".txt", "r+", encoding="utf-8")  # delete content of file and write new content in it
    except:
        f = open(os.path.dirname(pathlib.Path().resolve()) + "/Graph_Files/graph_" + layout + ".txt", "a", encoding="utf-8")   # create file for layout, if there isn't one
    #for k in range(0, 1000):
    f.truncate(0)
    charPos = wsg.getCharacterPositions()
    for word in lexicon:
        graphPoints, graphPointsNormalized = wsg.getWordGraphStepPoint(word, 20, charPos)
        if graphPoints == None: # there is a letter in the word, that is not on the keyboard and therefore no graph can be generated for this word and layout
            continue

        graphPointsNew = []
        for point in graphPoints:
            graphPointsNew.append(round(point[0], 5))
            graphPointsNew.append(round(point[1], 5))

        graphPointsNormalizedNew = []
        for point in graphPointsNormalized:
            graphPointsNormalizedNew.append(round(point[0], 5))
            graphPointsNormalizedNew.append(round(point[1], 5))
        f.write(word + ":")
        
        k = 0
        l = len(graphPointsNew)
        for i in graphPointsNew:
            k += 1
            f.write(str(i))
            if k < l:
                f.write(",")
        f.write(":")

        k = 0
        l = len(graphPointsNormalizedNew)
        for i in graphPointsNormalizedNew:
            k += 1
            f.write(str(i))
            if k < l:
                f.write(",")
        f.write("\n")
    f.close()

    print(time.time() - start_time)

if __name__ == "__main__":
    main()