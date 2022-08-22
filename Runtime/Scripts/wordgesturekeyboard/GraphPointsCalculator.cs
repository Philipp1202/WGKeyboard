using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace WordGestureKeyboard
{
  public class GraphPointsCalculator
  {
    private float _sigmaNormal;
    public bool isCalculatingPreview;
    public List<string> sortedDict;
    public string bestWord = "";

    /// <summary>
    /// Returns an array of two dictionaries with the words and their graph points (normalized and not), but only the ones where the starting and ending points lie close to the starting and ending points of the input.
    /// If the distance between starting and starting, and ending and ending points is too big, the word gets discarded and not returned in the dictionary.
    /// The calculations are performed with normalized points.
    /// </summary>
    /// <param name="input">List of points</param>
    /// <param name="locWordPoints">Dictionary with words and related graph points</param>
    /// <param name="normWordPoints">Dictionary with words and related normalized graph points</param>
    /// <param name="keyRadius">Radius of a key of the keyboard (normalized to keyboard length 1)</param>
    /// <returns>An array of two dictionaries with the words and their graph points (first entry's points are not normalized, second entry's points are normalized), but only the ones where the starting and ending points lie close to the starting and ending points of the input</returns>
    private Dictionary<string, List<Vector2>>[] PruneWithStartEndPos(List<Vector2> input,
      IReadOnlyDictionary<string, List<Vector2>> locWordPoints, Dictionary<string, List<Vector2>> normWordPoints,
      float keyRadius)
    {
      var newLocWordPoints = new Dictionary<string, List<Vector2>>();
      var newNormWordPoints = new Dictionary<string, List<Vector2>>();
      var steps = input.Count;
      var normInput = Normalize(input, 2);
      foreach (var word in normWordPoints.Where(word =>
                 (normInput[0] - word.Value[0]).magnitude < keyRadius * _sigmaNormal &&
                 (normInput[steps - 1] - word.Value[steps - 1]).magnitude < keyRadius * _sigmaNormal))
      {
        newLocWordPoints.Add(word.Key, locWordPoints[word.Key]);
        newNormWordPoints.Add(word.Key, word.Value);
      }

      return new[] {newLocWordPoints, newNormWordPoints};
    }

    /// <summary>
    /// Normalizes the given points by translating the centroid of the rectangle spanned by the points to the origin of the coordinate system and scaling the largest side to the given length.
    /// </summary>
    /// <param name="points">List of points</param>
    /// <param name="length">Length to which the largest side of the rectangle spanned by the given points should be scaled to</param>
    /// <returns>A List of normalized points</returns>
    public List<Vector2> Normalize(List<Vector2> points, int length)
    {
      var boundingBox = GetBounds(points);
      float[] boundingBoxSize = {boundingBox[1] - boundingBox[0], boundingBox[3] - boundingBox[2]};

      float s;
      if (Mathf.Max(boundingBoxSize[0], boundingBoxSize[1]) != 0)
      {
        s = length / Mathf.Max(boundingBoxSize[0], boundingBoxSize[1]);
      }
      else
      {
        s = float.MaxValue;
      }

      _sigmaNormal = s;

      var middlePoint = new Vector2((boundingBox[0] + boundingBox[1]) / 2, (boundingBox[2] + boundingBox[3]) / 2);

      return points.Select(point => (point - middlePoint) * s).ToList();
    }

    /// <summary>
    /// Calculates the proportional shape matching distance for every word given in "normalizedWordPointsDict" with respect to the "normalizedInputPoints" as in SHARK2.
    /// </summary>
    /// <param name="normalizedInputPoints">Normalized points (normalized user input)</param>
    /// <param name="normalizedWordPointsDict">Dictionary with all the normalized graph points for the words</param>
    /// <param name = "sigma" > Parameter for discarding words with to large distances</param>
    /// <returns>A dictionary with the words whose proportional shape matching distance is smaller than two times the key radius normalized and their related proportional shape matching distance</returns>
    private Dictionary<string, float> ShapeCosts(IReadOnlyList<Vector2> normalizedInputPoints,
      Dictionary<string, List<Vector2>> normalizedWordPointsDict, float sigma)
    {
      var steps = normalizedInputPoints.Count;
      var normalizedCostList = new Dictionary<string, float>();

      foreach (var pair in normalizedWordPointsDict)
      {
        var n = 0;
        float cost = 0;

        foreach (var p in pair.Value)
        {
          cost += (p - normalizedInputPoints[n]).magnitude;
          n += 1;
        }

        cost /= steps;

        if (cost <= 2 * sigma * _sigmaNormal)
        {
          normalizedCostList.Add(pair.Key, cost);
        }
      }

      return normalizedCostList;
    }

    /// <summary>
    /// Calculates the location channel distance for every word given in "locationWordPointsDict" with respect to the "inputpoints" as in SHARK2.
    /// </summary>
    /// <param name="inputPoints">Non-normalized points (user input)</param>
    /// <param name="locationWordPointsDict">Dictionary with all the non normalized graph points for all the words in the lexicon</param>
    /// <param name="sigma">Parameter for discarding words with to large distances</param>
    /// <param name="keyRadius">Radius of a key of the keyboard (normalized to keyboard length 1)</param>
    /// <returns>A dictionary with the words whose location channel distance is smaller than two times the key radius and their related location channel distance</returns>
    private static Dictionary<string, float> LocationCosts(List<Vector2> inputPoints,
      Dictionary<string, List<Vector2>> locationWordPointsDict, float sigma, float keyRadius)
    {
      var costList = new Dictionary<string, float>();
      var steps = inputPoints.Count;
      var alpha = new float[steps];
      for (var i = 0; i < Mathf.Ceil(steps / 2f); i++)
      {
        var a = Mathf.Abs(0.5f - (1f / steps) * i);
        alpha[i] = a + 10;
        alpha[steps - 1 - i] = a + 10;
      }

      var sum = alpha.Sum();
      for (var i = 0; i < steps; i++)
      {
        alpha[i] /= sum;
      }

      foreach (var word in locationWordPointsDict)
      {
        float cost = 0;
        float d = 0;
        float d2 = 0;
        float min;
        float m;
        foreach (var p in word.Value)
        {
          min = float.MaxValue;
          for (var i = 0; i < steps; i++)
          {
            m = (p - inputPoints[i]).magnitude;
            if (m < min)
            {
              min = m;
            }
          }

          d += Mathf.Max(min - keyRadius, 0);
        }

        foreach (var p in inputPoints)
        {
          min = float.MaxValue;
          for (var i = 0; i < steps; i++)
          {
            m = (p - word.Value[i]).magnitude;
            if (m < min)
            {
              min = m;
            }
          }

          d2 += Mathf.Max(min - keyRadius, 0);
        }

        if (d == 0 && d2 == 0)
        {
          cost = 0;
        }
        else
        {
          var k = 0;
          foreach (var p in word.Value)
          {
            cost += (p - inputPoints[k]).magnitude * alpha[k];
            k += 1;
          }
        }

        if (cost <= sigma * 2)
        {
          costList.Add(word.Key, cost);
        }
      }

      return costList;
    }

    /// <summary>
    /// Returns a list of characters with their corresponding cost.
    /// </summary>
    /// <param name="inputPoints">List of points</param>
    /// <param name="characterPositions">Dictionary of characters and their position on the WGKeyboard</param>
    /// <returns>A list of characters with their corresponding cost</returns>
    private static Dictionary<string, float> LocationCostsSingleCharacter(IReadOnlyCollection<Vector2> inputPoints,
      Dictionary<string, Vector2> characterPositions)
    {
      var costList = new Dictionary<string, float>();

      foreach (var character in characterPositions)
      {
        var d = inputPoints.Sum(p => (p - character.Value).magnitude);
        costList.Add(character.Key, d);
      }

      return costList;
    }

    /// <summary>
    /// Performs the channel integration applied in SHARK2.
    /// Returns a dictionary of words with their confidence score or null if no words are both in "shapeCosts" and in "locationCosts".
    /// </summary>
    /// <param name="shapeCosts">Dictionary of words with their proportional shape matching distance</param>
    /// <param name="locationCosts">Dictionary of words with their location channel distance</param>
    /// <param name="sigma">Parameter that is used to influence the two probabilities (for shape and location)</param>
    /// <returns>A dictionary of words with their confidence score or null</returns>
    private Dictionary<string, float> ChannelIntegration(Dictionary<string, float> shapeCosts,
      Dictionary<string, float> locationCosts, float sigma)
    {
      var wordList = (from word in shapeCosts where locationCosts.ContainsKey(word.Key) select word.Key).ToList();
      if (wordList.Count <= 0) return null;
      var shapeProbabilities = new Dictionary<string, float>();
      foreach (var pair in shapeCosts)
      {
        var shapeProb = 1 / ((sigma * _sigmaNormal) * Mathf.Sqrt(2 * Mathf.PI)) *
                        Mathf.Exp((float) (-0.5 * Mathf.Pow(pair.Value / (sigma * _sigmaNormal), 2)));
        shapeProbabilities[pair.Key] = shapeProb;
      }

      var locationProbabilities = new Dictionary<string, float>();
      foreach (var pair in locationCosts)
      {
        var locationProb = 1 / (sigma * Mathf.Sqrt(2 * Mathf.PI)) *
                           Mathf.Exp((float) (-0.5 * Mathf.Pow(pair.Value / sigma, 2)));
        locationProbabilities[pair.Key] = locationProb;
      }

      var shapeSum = shapeProbabilities.Sum(pair => pair.Value);
      var locationSum = locationProbabilities.Sum(pair => pair.Value);

      var shapeProbabilities2 = new Dictionary<string, float>();
      foreach (var pair in shapeProbabilities)
      {
        shapeProbabilities2[pair.Key] = shapeProbabilities[pair.Key] / shapeSum;
      }

      var locationProbabilities2 = new Dictionary<string, float>();
      foreach (var pair in locationProbabilities)
      {
        locationProbabilities2[pair.Key] = locationProbabilities[pair.Key] / locationSum;
      }

      var confidenceScores = new Dictionary<string, float>();
      var sum = wordList.Sum(word => shapeProbabilities2[word] * locationProbabilities2[word]);
      foreach (var word in wordList)
      {
        confidenceScores[word] = shapeProbabilities2[word] * locationProbabilities2[word] / sum;
      }

      return confidenceScores;
    }

    /// <summary>
    /// Calculates which words are most likely to be the ones, the user intended to write.
    /// </summary>
    /// <param name="userInputPoints">List of points for which the best words should be found</param>
    /// <param name="locationWordPointsDict">Dictionary with all the non normalized graph points for all the words in the lexicon</param>
    /// <param name="normalizedWordPointsDict">Dictionary with all the normalized graph points for all the words in the lexicon</param>
    /// <param name="sigma">Parameter for algorithm, to determine if words should be discarded early or not. If smaller, more words get discarded early on. Also needed for calculation of confidence score</param>
    /// <param name="keyRadius">Radius of a key of the keyboard (normalized to keyboard length 1)</param>
    /// <param name="wordRanking">Dictionary of words and their rank (according to frequency in language)</param>
    /// <param name="isWriting">Is the user currently writing or did they end the gesture</param>
    /// <returns>A List of the best scoring words</returns>
    public async void CalcBestWords(List<Vector2> userInputPoints,
      Dictionary<string, List<Vector2>> locationWordPointsDict,
      Dictionary<string, List<Vector2>> normalizedWordPointsDict, float sigma, float keyRadius,
      Dictionary<string, int> wordRanking, bool isWriting)
    {
      await Task.Run(() =>
      {
        if (isWriting)
        {
          isCalculatingPreview = true;
        }

        sortedDict = null;
        var steps = locationWordPointsDict[locationWordPointsDict.Keys.First()].Count;
        var inputPoints = GetWordGraphStepPoints(userInputPoints, steps);
        var filteredWordPointsArr =
          PruneWithStartEndPos(inputPoints, locationWordPointsDict, normalizedWordPointsDict, keyRadius);

        if (IsSingleCharacter(userInputPoints, keyRadius))
        {
          // short gesture, indicates that user wants to write a single character
          var characterPositions = new Dictionary<string, Vector2>();
          foreach (var pair in filteredWordPointsArr[0].Where(pair => pair.Key.Length == 1))
          {
            characterPositions[pair.Key] = pair.Value[0];
          }

          var distances = LocationCostsSingleCharacter(inputPoints, characterPositions);
          var lowestDistChar = distances.OrderByDescending(x => x.Value).Last().Key;
          var charPos = characterPositions[lowestDistChar];
          if (inputPoints.Any(p =>
                charPos.x + keyRadius < p.x || charPos.x - keyRadius > p.x || charPos.y + keyRadius < p.y ||
                charPos.y - keyRadius > p.y))
          {
            if (!isWriting) return;
            bestWord = "";
            isCalculatingPreview = false;
            return; // even closest character is too far away to be the one the user intended to write
          }

          if (isWriting)
          {
            bestWord = lowestDistChar;
            isCalculatingPreview = false;
          }
          else
          {
            sortedDict = new List<string> {lowestDistChar};
          }
        }
        else
        {
          var normalizedInputPoints = Normalize(inputPoints, 2);
          var shapeCosts = ShapeCosts(normalizedInputPoints, filteredWordPointsArr[1], sigma);
          var locationCosts = LocationCosts(inputPoints, filteredWordPointsArr[0], sigma, keyRadius);
          var confidenceScores = ChannelIntegration(shapeCosts, locationCosts, sigma);

          if (confidenceScores != null)
          {
            var sortedList = new List<string>();
            if (confidenceScores.Count == 1)
            {
              sortedList = confidenceScores.Keys.ToList();
            }
            else
            {
              var sortedConfidenceScores = from entry in confidenceScores orderby entry.Value descending select entry;

              var sortedConfidenceScoresList = sortedConfidenceScores.ToList();
              var sort = new Dictionary<string, int>();
              IOrderedEnumerable<KeyValuePair<string, int>> sorted;
              var highestValue = sortedConfidenceScoresList[0].Value;
              var isSorted = false;
              foreach (var pair in sortedConfidenceScoresList)
              {
                if (pair.Value == highestValue)
                {
                  // same confidence score, compare word rank
                  sort[pair.Key] = wordRanking[pair.Key];
                }
                else
                {
                  // sort words with same confidence score by word rank, add them to a list of sorted words and look for the next highest confidence score
                  sorted = from entry in sort orderby entry.Value select entry;
                  sortedList.AddRange(sorted.Select(entry => entry.Key));
                  if (sortedList.Count >= 5)
                  {
                    // only need the best match plus four words to choose from.
                    isSorted = true;
                    break;
                  }

                  sort.Clear();
                  sort[pair.Key] = wordRanking[pair.Key];
                  highestValue = pair.Value;
                }
              }

              if (!isSorted)
              {
                sorted = from entry in sort orderby entry.Value ascending select entry;
                sortedList.AddRange(sorted.Select(entry => entry.Key));
              }
            }

            if (isWriting)
            {
              bestWord = sortedList[0];
              isCalculatingPreview = false;
            }
            else
            {
              sortedDict = sortedList;
            }
          }
          else
          {
            if (isWriting)
            {
              bestWord = "";
              isCalculatingPreview = false;
            }
            else
            {
              sortedDict = new List<string> {""};
            }
          }
        }
      });
    }

    /// <summary>
    /// Returns a list of points for a given word or null if it cannot be written with the given layout.
    /// These points correspond to the perfect graph points of this given word.
    /// </summary>
    /// <param name="word">Word for which the graph points have to be determined</param>
    /// <param name="layoutInfo">Tuple that contains a list of lines of characters from a layout and their corresponding indents</param>
    /// <returns>A list of points for a given word or null if it cannot be written with the given layout</returns>
    public List<Vector2> GetWordPoints(string word, Tuple<List<float>, List<string>> layoutInfo)
    {
      var letterPos = new Dictionary<string, Vector2>();
      var indentList = layoutInfo.Item1;
      var charList = layoutInfo.Item2;
      var count = charList.Count;

      var allCharacters = new HashSet<char>();
      foreach (var character in from line in charList
               from character in line
               where character.ToString() != " " && character.ToString() != "<"
               where !allCharacters.Contains(character)
               select character)
      {
        allCharacters.Add(character);
      }

      if (word.Any(c => !allCharacters.Contains(c)))
      {
        return null; // word cannot be written with current layout
      }

      float numKeysOnLongestLine = 0;
      for (var i = 0; i < count; i++)
      {
        var lineLength = charList[i].Length + Mathf.Abs(indentList[i]);
        if (charList[i].Contains(" "))
        {
          lineLength += 7; // because length of spacebar is 8 * normal keysize, that means 7 * keysize extra
        }

        if (charList[i].Contains("<"))
        {
          lineLength += 1; // because length of backspace is 2 * normal keysize, that means 1 * keysize extra
        }

        if (lineLength > numKeysOnLongestLine)
        {
          numKeysOnLongestLine = lineLength;
        }
      }

      for (var y = 0; y < count; y++)
      {
        var offset = indentList[count - 1 - y];
        for (var x = 0; x < charList[count - y - 1].Count(); x++)
        {
          switch (charList[count - y - 1][x].ToString())
          {
            case " ":
              offset += 7;
              continue;
            case "<":
              offset += 1;
              continue;
            default:
              letterPos.Add(charList[count - y - 1][x].ToString(),
                new Vector2((x + offset + 0.5f) / numKeysOnLongestLine, (y + 0.5f) / numKeysOnLongestLine));
              break;
          }
        }
      }

      var points = word.Select(letter => letterPos[letter.ToString()]).ToList();

      var locationPoints = GetWordGraphStepPoints(points, 20);
      return locationPoints;
    }

    /// <summary>
    /// Checks if for the given points it is very likely that the input is a single character and not a word.
    /// </summary>
    /// <param name="points">List of points</param>
    /// <param name="keyRadius">Radius of a key of the keyboard (normalized to keyboard length 1)</param>
    /// <returns>True if the largest side of the rectangle spanned by the points is smaller than or equal to the radius of a key, false otherwise</returns>
    private static bool IsSingleCharacter(List<Vector2> points, float keyRadius)
    {
      var bounds = GetBounds(points);
      return !(bounds[1] - bounds[0] > keyRadius) && !(bounds[3] - bounds[2] > keyRadius);
    }

    /// <summary>
    /// Checks if for the given points, the input is a backspace, space or something else.
    /// Returns -1 for a backspace, 1 for space and 0 if it is none of the two.
    /// </summary>
    /// <param name="points">List of points to check if they are all in the backspace or space hit box</param>
    /// <param name="backSpaceHitBox">Hit box of the backspace key.</param>
    /// <param name="spaceHitBox">Hit box of the space key.</param>
    /// <returns>-1 for a backspace, 1 for space and 0 if it is none of the two</returns>
    public static int IsBackSpaceOrSpace(List<Vector2> points, float[] backSpaceHitBox, float[] spaceHitBox)
    {
      if (points.Count == 0)
      {
        return 0;
      }

      var bounds = GetBounds(points);
      if (backSpaceHitBox[0] <= bounds[0] && backSpaceHitBox[1] >= bounds[1] && backSpaceHitBox[2] <= bounds[2] &&
          backSpaceHitBox[3] >= bounds[3])
      {
        return -1;
      }

      if (spaceHitBox[0] <= bounds[0] && spaceHitBox[1] >= bounds[1] && spaceHitBox[2] <= bounds[2] &&
          spaceHitBox[3] >= bounds[3])
      {
        return 1;
      }

      return 0;
    }

    /// <summary>
    /// Returns a List of #steps points that all have the same distance to their subsequent point and lie on the graph the points in the "points" list span.
    /// </summary>
    /// <param name="points">List of points</param>
    /// <param name="steps">Number of points the returned list has to contain</param>
    /// <returns>A List of #steps points that all have the same distance to their subsequent point and lie on the graph the points in the "points" list span</returns>
    private List<Vector2> GetWordGraphStepPoints(List<Vector2> points, int steps)
    {
      var length = GetLengthBetweenPoints(points);
      var stepPoints = new List<Vector2>();
      var currPos = points[0];

      // "steps" has to be at least 2, otherwise it does not make any sense
      if (steps <= 1)
      {
        steps = 2;
      }

      // "points" is a list with only one point
      if (length == 0)
      {
        for (var i = 0; i < steps; i++)
        {
          stepPoints.Add(currPos);
        }

        return stepPoints;
      }

      var stepSize = length / (steps - 1);
      var distVectors = new List<Vector2>();
      for (var i = 0; i < points.Count - 1; i++)
      {
        distVectors.Add(points[i + 1] - points[i]);
      }

      var numSteps = 1;
      var currStep = stepSize;
      var currPosNum = 0;
      var currDistVecNum = 0;

      stepPoints.Add(currPos);

      while (numSteps < steps)
      {
        if (currDistVecNum >= distVectors.Count)
        {
          stepPoints.Add(
            currPos); // adding the last point "manually", because sometimes it can happen (because the numbers can get very small), that there are some rounding errors and it would want to go on the next distance vector that doesn't exist. The distance it would have to go further is about 1*e^(-8), which can be ignored.
          numSteps += 1;
        }
        else
        {
          var distVec = distVectors[currDistVecNum];
          double distVecLength = Mathf.Sqrt(Mathf.Pow(distVec[0], 2) + Mathf.Pow(distVec[1], 2));
          if (currStep != stepSize)
          {
            if (distVecLength - currStep > -0.0000001)
            {
              numSteps += 1;
              currPos = currPos + distVec / (float) distVecLength * (float) currStep;
              distVectors[currDistVecNum] = points[currPosNum + 1] - currPos;
              stepPoints.Add(currPos);
              currStep = stepSize;
            }
            else
            {
              currStep -= distVecLength;
              currDistVecNum += 1;
              currPosNum += 1;
              currPos = points[currPosNum];
            }
          }
          else if ((int) (distVecLength / stepSize + 0.0000001) > 0)
          {
            var numPointsOnLine = (int) (distVecLength / stepSize + 0.0000001);
            numSteps += numPointsOnLine;
            for (var i = 0; i < numPointsOnLine; i++)
            {
              stepPoints.Add(currPos + distVec / (float) distVecLength * ((i + 1) * (float) stepSize));
            }

            if (distVecLength - numPointsOnLine * stepSize > 0.0000001)
            {
              currStep -= (distVecLength - numPointsOnLine * stepSize);
            }

            currDistVecNum += 1;
            currPosNum += 1;
            currPos = points[currPosNum];
          }
          else
          {
            currStep -= distVecLength;
            currDistVecNum += 1;
            currPosNum += 1;
            currPos = points[currPosNum];
          }
        }
      }

      return stepPoints;
    }

    /// <summary>
    /// Returns the added up distance of all the distances between two adjacent points.
    /// </summary>
    /// <param name="points">List of points</param>
    /// <returns>The added up distance of all the distances between two adjacent points</returns>
    private static double GetLengthBetweenPoints(IReadOnlyList<Vector2> points)
    {
      double dist = 0;
      for (var i = 0; i < points.Count - 1; i++)
      {
        var distVec = points[i] - points[i + 1];
        dist += Mathf.Sqrt(Mathf.Pow(distVec[0], 2) + Mathf.Pow(distVec[1], 2));
      }

      return dist;
    }

    /// <summary>
    /// Returns the x coordinates for all the given points.
    /// </summary>
    /// <param name="points">List of points</param>
    /// <returns>A List of the x coordinates of all the given points</returns>
    private static List<float> GetX(IReadOnlyList<Vector2> points)
    {
      var xPoints = new List<float>();
      for (var i = 0; i < points.Count; i++)
      {
        xPoints.Add(points[i][0]);
      }

      return xPoints;
    }

    /// <summary>
    /// Returns the y coordinates for all the given points.
    /// </summary>
    /// <param name="points">List of points</param>
    /// <returns>A List of the y coordinates of all the given points</returns>
    private static List<float> GetY(IReadOnlyList<Vector2> points)
    {
      var yPoints = new List<float>();
      for (var i = 0; i < points.Count; i++)
      {
        yPoints.Add(points[i][1]);
      }

      return yPoints;
    }

    /// <summary>
    /// Returns an array with the minimal x, maximal x, minimal y and maximal y over all the points.
    /// </summary>
    /// <param name="points">List of points</param>
    /// <returns>An array with the minimal x, maximal x, minimal y and maximal y over all the points</returns>
    private static float[] GetBounds(List<Vector2> points)
    {
      var xList = GetX(points);
      var yList = GetY(points);

      var minX = xList.Min();
      var maxX = xList.Max();
      var minY = yList.Min();
      var maxY = yList.Max();

      return new[] {minX, maxX, minY, maxY};
    }
  }
}