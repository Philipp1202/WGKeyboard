using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Linq;

namespace WordGestureKeyboard
{
  public class FileHandler
  {
    private Dictionary<string, List<Vector2>> _locationWordPointsDict;
    private Dictionary<string, List<Vector2>> _normalizedWordPointsDict;
    public string layout;
    public List<string> layouts;
    private Dictionary<string, Tuple<List<float>, List<string>>> _layoutCompositions;
    private readonly HashSet<string> _wordsInLexicon;
    public readonly Dictionary<string, int> wordRanking;
    public bool isLoading;
    private const string PathToAssets = "Packages/ch.unibas.wgkeyboard/Assets/";
    private const string PathToLexicon = PathToAssets + "lexicon/lexicon.txt";

    public FileHandler(string layout)
    {
      this.layout = layout;
      _wordsInLexicon = GetAllWordsInLexicon().Item1;
      wordRanking = GetAllWordsInLexicon().Item2;
    }

    /// <summary>
    /// Loads the points for the perfect graphs for the words that have been generated for a certain keyboard layout in a file called "graph_'layout'.txt".
    /// Saves the information in the locationWordPointsDict and normalizedWordPointsDict.
    /// </summary>
    /// <param name="loadLayout">Layout from which the graphs should be loaded</param>
    public void LoadWordGraphs(string loadLayout)
    {
      _locationWordPointsDict = new Dictionary<string, List<Vector2>>();
      _normalizedWordPointsDict = new Dictionary<string, List<Vector2>>();

      var path = $"{PathToAssets}Graph_Files/graph_{loadLayout}.txt";

      foreach (var line in File.ReadLines(path))
      {
        var locationPoints = new List<Vector2>();
        var normalizedPoints = new List<Vector2>();

        var splits = line.Split(":");
        var points = splits[1].Split(",");
        var normalized = splits[2].Split(","); // normalized points
        for (var i = 0; i < points.Length; i += 2)
        {
          locationPoints.Add(new Vector2(float.Parse(points[i]), float.Parse(points[i + 1])));
        }

        for (var i = 0; i < normalized.Length; i += 2)
        {
          normalizedPoints.Add(new Vector2(float.Parse(normalized[i]), float.Parse(normalized[i + 1])));
        }

        _locationWordPointsDict.Add(splits[0], locationPoints);
        _normalizedWordPointsDict.Add(splits[0], normalizedPoints);
      }

      isLoading = false;
    }

    /// <summary>
    /// Checks if the word "newWord" already exists in the dictionary. If true, then this function does nothing, else it
    /// adds the word and graphs to the normalizedWordPointsDict and locationWordPointsDict, such that they can be immediately used with the active layout.
    /// </summary>
    /// <param name="newWord">New word that should be added to the dictionary.</param>
    /// <param name="gpc">Graph points calculator to calculate the graph points.</param>
    public void AddNewWordToDict(string newWord, GraphPointsCalculator gpc)
    {
      if (newWord.Length == 0 || newWord.Contains(" ") || _wordsInLexicon.Contains(newWord)) return;

      foreach (var l in layouts)
      {
        var wordLocationPoints = gpc.GetWordPoints(newWord, _layoutCompositions[l]);
        if (wordLocationPoints == null)
        {
          continue; // word cannot be written with given layout
        }

        var wordNormPoints = gpc.Normalize(wordLocationPoints, 2);

        if (l != layout) continue;
        // add word and points to these two dictionaries so that this word can be written as gesture with the active layout without reloading
        _normalizedWordPointsDict.Add(newWord, wordNormPoints);
        _locationWordPointsDict.Add(newWord, wordLocationPoints);
      }

      _wordsInLexicon.Add(newWord);
      wordRanking[newWord] = 100;
    }

    /// <summary>
    /// Loads the layouts from the file in Assets/layouts.txt.
    /// It stores the layout names in "layouts" and the order of the characters for a line with the indent for every layout in the dictionary "layoutCompositions".
    /// </summary>
    public void LoadLayouts()
    {
      layouts = new List<string>();
      _layoutCompositions = new Dictionary<string, Tuple<List<float>, List<string>>>();
      const string path = PathToAssets + "layouts.txt";
      var l = ""; // layout name
      var composition = new Tuple<List<float>, List<string>>(new List<float>(), new List<string>());
      foreach (var line in File.ReadLines(path).Skip(6))
      {
        if (l == "")
        {
          l = line;
        }
        else if (line == "-----")
        {
          var allCharacters = new HashSet<char>();
          foreach (var character in composition.Item2.SelectMany(s =>
                     s.ToLower().Where(character => character.ToString() != " " && character.ToString() != "<")))
          {
            if (allCharacters.Contains(character))
            {
              goto Illegal;
            }

            allCharacters.Add(character);
          }

          float longestKeyboardLine = 0;
          for (var j = 0; j < composition.Item2.Count; j++)
          {
            var lineLength = composition.Item2[j].Length + Mathf.Abs(composition.Item1[j]);
            if (composition.Item2[j].Contains(" "))
            {
              lineLength += 7; // because length of spacebar is 8 * normal keysize, that means 7 * keysize extra
            }

            if (composition.Item2[j].Contains("<"))
            {
              lineLength += 1; // because length of backspace is 2 * normal keysize, that means 1 * keysize extra
            }

            if (lineLength > longestKeyboardLine)
            {
              longestKeyboardLine = lineLength;
            }
          }

          var isWiderThanHigh = longestKeyboardLine >= composition.Item2.Count;

          if (File.Exists(PathToAssets + "Graph_Files/graph_" + l + ".txt") && isWiderThanHigh)
          {
            // graph file does not exist / layout is higher than wide
            layouts.Add(l);
            _layoutCompositions.Add(l, composition);
          }

          Illegal:
          composition = new Tuple<List<float>, List<string>>(new List<float>(), new List<string>());
          l = "";
        }
        else
        {
          var splits = line.Split("$$");
          if (float.TryParse(splits[1], out var f))
          {
            // true, if a float, false if not
            composition.Item1.Add(f);
          }
          else
          {
            composition.Item1.Add(0);
          }

          composition.Item2.Add(splits[0]);
        }
      }
    }

    /// <summary>
    /// Returns a tuple containing a hashset of all words in the lexicon and a dictionary with all the words and their
    /// rank (how frequently used in language)
    /// </summary>
    /// <returns>The words in a lexicon and their rank</returns>
    private static Tuple<HashSet<string>, Dictionary<string, int>> GetAllWordsInLexicon()
    {
      var words = new HashSet<string>();
      var wordsWithRank = new Dictionary<string, int>();
      var rank = 0;
      foreach (var line in File.ReadLines(PathToLexicon))
      {
        words.Add(line);
        wordsWithRank[line] = rank;
        rank++;
      }

      return new Tuple<HashSet<string>, Dictionary<string, int>>(words, wordsWithRank);
    }

    public Dictionary<string, Tuple<List<float>, List<string>>> GetLayoutCompositions()
    {
      return _layoutCompositions;
    }

    public Dictionary<string, List<Vector2>> GetLocationWordPointsDict()
    {
      return _locationWordPointsDict;
    }

    public Dictionary<string, List<Vector2>> GetNormalizedWordPointsDict()
    {
      return _normalizedWordPointsDict;
    }
  }
}