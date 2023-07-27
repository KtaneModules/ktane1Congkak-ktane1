using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Text.RegularExpressions;

public class BORDGAEMXD : MonoBehaviour
{
	public KMBombInfo Bomb;
	public KMBombModule Module;
	public KMAudio Audio;

	public KMSelectable[] yourHoles;
	public KMSelectable[] theirHoles;
	public GameObject[] seedsPlacement;
	public GameObject[] pivot;
	public Material[] colours;
	public Renderer ReferenceSeed;
	public TextMesh screenText;

	private int[] seeds = Enumerable.Repeat(7, 16).ToArray();
	private int[] initialState = new int[16];

	private int[] mostSeeds = new int[7];

	private bool turn;
	private bool codeLogging;

	static int moduleIdCounter = 1;
	int moduleId;
	private bool moduleSolved = false;
	string[] funnyPhrases = {"What are you doing. Stop.",
							 "No, you can't eat those \"seeds\"",
							 "Please, go solve the rest of the bomb; this module is already solved",
							 "What do you want from me? TwT",
							 "If you REALLY are bored, better check THIS out: https://www.youtube.com/watch?v=o-YBDTqX_ZU"};

	void Awake ()
	{
		moduleId = moduleIdCounter++;
		for (int i = 0; i < yourHoles.Length; i++)
		{
			int j = i;
			yourHoles[j].OnInteract += () => { holeHandler(j); return false; };

            yourHoles[i].OnHighlight += delegate ()
            {
                if (!moduleSolved)
                {
                    highlightText(j, true);
                }
            };

            yourHoles[i].OnHighlightEnded += delegate ()
            {
                if (!moduleSolved)
                {
                    highlightText(j, false);
                }
            };
        }
        for (int i = 0; i < theirHoles.Length; i++)
		{
            int j = i;
            theirHoles[i].OnHighlight += delegate ()
            {
                if (!moduleSolved)
                {
                    highlightText(j + 8, true);
                }
            };

            theirHoles[i].OnHighlightEnded += delegate ()
            {
                if (!moduleSolved)
                {
                    highlightText(j + 8, false);
                }
            };
        }
    }

    void Start ()
	{
        screenText.text = "";
        distribute();
		logArray(seeds, "Initial state:");
		Array.Copy(getTheMost(), mostSeeds, mostSeeds.Length);
		logArray(mostSeeds, "Total points gained after playing each hole (sum of seeds in player's holes + extra seeds in the store, if any):");
		Debug.LogFormat("[Congkak #{0}] The maximum seeds in a hole is: {1}, which is in the hole: {2}.", moduleId, mostSeeds.Max(), ((Array.IndexOf(mostSeeds, mostSeeds.Max())) + 1));
	}

	void logArray (int[] arr, string extraMsg = "")
	{
		string str = "";
		for (int i = 0; i < arr.Length; i++)
			str += arr[i] + " ";

		Debug.LogFormat("[Congkak #{0}] {1} {2}", moduleId, extraMsg, str);
	}

	void distribute ()
	{
		seeds[7] = 0;
		seeds[15] = 0;
		int rng;
		int turns = UnityEngine.Random.Range(8, 15 + 1); //range: [x, y]
		turn = turns % 2 != 0;
		for (int j = 0; j <= turns; j++)
		{
			do rng = UnityEngine.Random.Range(getBoundsFromPlayer(turn, false), getBoundsFromPlayer(!turn, true));
			while (seeds[rng] <= 0);

			playAturn(rng, turn);
			turn = !turn;
		}
		//In case for any potential error cases
		//seeds = new int[] { 2, 3, 1, 1, 0, 4, 0, 31, 0, 4, 0, 1, 0, 1, 2, 48, };
		Array.Copy(seeds, initialState, seeds.Length);
        colorSeeds();
		positionSeeds();
	}

    void positionSeeds()
    {
        float sphereRadius = 4.2f;
        float seedRadius = .5f * .002f;
        float circleRadius = sphereRadius * (2 / 3f);
        int seedCount = 0;
        Vector3 posToCheck;
        List<SphereCollider> placedColliders;
        for (int k = 0; k < 16; k++)
        {
            placedColliders = new List<SphereCollider>();
            var cupPos = pivot[k].transform;
            bool seedDiff = k % 8 == 7;
            for (int l = 0; l < seeds[k]; l++)
            {
                var seed = seedsPlacement[seedCount++].transform;
                seed.parent = cupPos;
            redo:
                var randLoc = UnityEngine.Random.insideUnitCircle * circleRadius;
                float xDiff = (randLoc.x * randLoc.x);
                float yDiff = (randLoc.y * randLoc.y);
                if (seedDiff) { xDiff /= 1.5f * 1.5f; yDiff /= 1.95f * 1.95f; }
                float sqr = ((sphereRadius * sphereRadius) - xDiff - yDiff);
                float resultZ = -Mathf.Sqrt(Mathf.Abs(sqr));
                posToCheck = new Vector3(randLoc.x, resultZ + seedRadius, randLoc.y);
                seed.localPosition = posToCheck;
                for (int b = 0; b < placedColliders.Count; b++)
                {
                    if (spheresOverlap(placedColliders[b], seed.gameObject.GetComponent<SphereCollider>()))
                        goto redo;
                }
                placedColliders.Add(seed.gameObject.GetComponent<SphereCollider>());
            }
        }
    }

    bool spheresOverlap(SphereCollider a, SphereCollider b)
    {
        float dist = Vector3.Distance(a.transform.localPosition, b.transform.localPosition);
        return dist < a.radius + b.radius;
    }

    void colorSeeds()
    {
		for (int i = 0; i < 98; i++)
        {
			int n = UnityEngine.Random.Range(0, 10);
			seedsPlacement[i].GetComponent<MeshRenderer>().material = colours[n];
        }
    }

    int int2bool (bool originalBool)
	{
		return originalBool ? 1 : 0;
	}

	int getBoundsFromPlayer (bool player, bool upperBound)
	{ //P0 is the human; P1 is the machine; P = player
		if (upperBound) return int2bool(!player) * 8 + 7; // = player's store/main hole
		else return int2bool(player) * 8;
	}

	int[] getTheMost ()
	{
		int[] result = new int[7];

		for (int holeNumber = 0; holeNumber < 7; holeNumber++)
		{
			if (seeds[holeNumber] > 0)
			{
				if (codeLogging) Debug.LogFormat("[Congkak #{0}] ----------------- PLAYING HOLE {1}:", moduleId, (holeNumber + 1));
				playAturn(holeNumber, false);
				if (codeLogging) Debug.LogFormat("[Congkak #{0}] Total number of seeds: {1} for hole: {2}", moduleId, Enumerable.Sum(seeds), (holeNumber + 1)); //debug
																															  //this should ALWAYS be 98
				getTotalPts(holeNumber, result);
				reset();
			}
		}
		return result;
	}

	void playAturn (int hn, bool turn)
	{
		int[] bounds = { getBoundsFromPlayer(turn, false), getBoundsFromPlayer(turn, true), getBoundsFromPlayer(!turn, false), getBoundsFromPlayer(!turn, true) };
		// = {lower bound from current player, upper bound from current player, lower bound from the opponent, upper bound from the opponent}

		do
		{
			int hold = seeds[hn];
			seeds[hn] = 0;
			for (; hold > 0; hold--)
			{ //Distrubuting seeds
				hn++;
				if (hn == bounds[3]) hn = bounds[2]; //Skipping opponent's store/HoleNumber
				else if (hn >= 16) hn = 0;
				seeds[hn]++;
			}
			if (codeLogging) { Debug.LogFormat("[Congkak #{0}] Ended in hole: {1}", moduleId, (hn + 1)); logArray(seeds); }
			if (seeds[hn] == 1 && hn >= bounds[0] && hn < bounds[3])
			{// Accounting for when last seed lands on own empty hole
				seeds[bounds[1]] += seeds[hn] + seeds[14 - hn];
				seeds[hn] = 0;
				seeds[14 - hn] = 0;
				if (codeLogging) { Debug.LogFormat("[Congkak #{0}] Capturing hole {1}", moduleId, (14 - hn + 1)); logArray(seeds); }
				break;
			}
		}
		while (!(hn == bounds[1] || seeds[hn] == 1));// When last seed lands on player's store or in an empty hole

    }

    void playHole(int hole)
	{
        int a = UnityEngine.Random.Range(1, 5);
        Audio.PlaySoundAtTransform("sound" + a.ToString(), transform);
        playAturn(hole, false);
        int rng;
        do rng = UnityEngine.Random.Range(getBoundsFromPlayer(true, false), getBoundsFromPlayer(false, true));//Machine playing
        while (seeds[rng] <= 0);
        playAturn(rng, true);
        Array.Copy(seeds, initialState, seeds.Length);//Copying new state as initial
        positionSeeds();//Repositioning seeds
		if (!moduleSolved)
		{
            logArray(seeds, "Initial state:");
            Array.Copy(getTheMost(), mostSeeds, mostSeeds.Length);
            logArray(mostSeeds, "Total points gained after playing each hole:");
            Debug.LogFormat("[Congkak #{0}] The maximum seeds in a hole is: {1}, which is in the hole: {2}.", moduleId, mostSeeds.Max(), ((Array.IndexOf(mostSeeds, mostSeeds.Max())) + 1));
        }
    }

	void getTotalPts (int hn, int[] result)
	{
		for (int i = 0; i < 7; i++)
			result[hn] += seeds[i];
		result[hn] += seeds[seeds.Length - 1] - initialState[seeds.Length - 1];
		if (codeLogging) Debug.LogFormat("[Congkak #{0}] Points gained if the player played this hole: {1} with value {2}", moduleId, result[hn], initialState[hn]);
	}

	void reset ()
	{
		Array.Copy(initialState, seeds, initialState.Length);
	}

	void holeHandler (int hole)
	{
		if (moduleSolved == false)
		{
			if (mostSeeds[hole] == mostSeeds.Max())
			{
				moduleSolved = true;
				Debug.LogFormat("[Congkak #{0}] You selected the correct hole (Hole #{1}), which returns the maximum of {2} seeds. Module solved.", moduleId, hole + 1, mostSeeds[hole]);
                screenText.text = "GG";
				playHole(hole);
                Module.HandlePass();
			}
			else if (initialState[hole] != 0)
			{
                if (initialState[hole] == initialState[Array.IndexOf(mostSeeds, mostSeeds.Where(x => x != 0).Min())])
				{
                    Debug.LogFormat("[Congkak #{0}] You selected the wrong hole (Hole #{1}), which returns the minimum of {2} seeds. Strike, and game continues.", moduleId, hole + 1, mostSeeds[hole]);
                    Module.HandleStrike();
                }
				else
				{
                    Debug.LogFormat("[Congkak #{0}] You selected the wrong hole (Hole #{1}), which returns {2} seeds. Game continues.", moduleId, hole + 1, mostSeeds[hole]);
                }
                playHole(hole);
            }
			else
			{
                Debug.LogFormat("[Congkak #{0}] You selected an empty hole (Hole #{1}). Strike.", moduleId, hole + 1);
                Module.HandleStrike();
            }
        }
		else
			Debug.LogFormat(funnyPhrases[UnityEngine.Random.Range(0, funnyPhrases.Length)]);
	}

	void highlightText(int hole, bool isHighlight)
	{
		if (isHighlight)
		{
            screenText.text = initialState[hole].ToString("00");
        }
		else
		{
            screenText.text = "";
        }
    }

    //twitch plays
	#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} hole <#> [Chooses the hole in the specified position '#' from bottom-right to top-left (1-7)], !{0} highlight <#> [Highlights the hole in specified position '#' from bottom-right going clockwise around the board disregarding big holes (1-15)], !{0} cycle [Highlights each hole from bottom-right going clockwise disregarding big holes]" ;
	#pragma warning restore 414
	IEnumerator ProcessTwitchCommand(string command)
	{
        string[] parameters = command.Split(' ');
        if (Regex.IsMatch(command, @"^\s*cycle\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
            var waitTime = 0.9f;
            for (int i = 0; i < yourHoles.Length; i++)
            {
                yield return "trycancel";
                yourHoles[i].OnHighlight();
                yield return new WaitForSeconds(waitTime);
                yourHoles[i].OnHighlightEnded();
                yield return new WaitForSeconds(0.1f);
            }
            for (int i = 0; i < theirHoles.Length; i++)
            {
                yield return "trycancel";
                theirHoles[i].OnHighlight();
                yield return new WaitForSeconds(waitTime);
                theirHoles[i].OnHighlightEnded();
                yield return new WaitForSeconds(0.1f);
            }
        }
		else if (Regex.IsMatch(parameters[0], @"^\s*hole\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
		{
			yield return null;
			if (parameters.Length > 2)
			{
				yield return "sendtochaterror Too many parameters!";
			}
			else if (parameters.Length == 2)
			{
				int temp = -1;
				if (!int.TryParse(parameters[1], out temp))
				{
					yield return "sendtochaterror The specified position '" + parameters[1] + "' is invalid!";
					yield break;
				}
				if (temp < 1 || temp > 7)
				{
					yield return "sendtochaterror The specified position '" + parameters[1] + "' is out of range 1-7!";
					yield break;
				}
				yourHoles[temp - 1].OnInteract();
			}
			else if (parameters.Length == 1)
			{
				yield return "sendtochaterror Please specify the position of the hole you wish to choose!";
			}
			yield break;
		}
        else if (Regex.IsMatch(parameters[0], @"^\s*highlight\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            yield return null;
            if (parameters.Length > 2)
            {
                yield return "sendtochaterror Too many parameters!";
            }
            else if (parameters.Length == 2)
            {
                int temp = -1;
                if (!int.TryParse(parameters[1], out temp))
                {
                    yield return "sendtochaterror The specified position '" + parameters[1] + "' is invalid!";
                    yield break;
                }
                if (temp < 1 || temp > 15)
                {
                    yield return "sendtochaterror The specified position '" + parameters[1] + "' is out of range 1-7!";
                    yield break;
                }
				if (temp < 8)
				{
                    yourHoles[temp - 1].OnHighlight();
                    yield return new WaitForSeconds(0.9f);
                    yourHoles[temp - 1].OnHighlightEnded();
                    yield return new WaitForSeconds(0.1f);
                }
				else
				{
                    theirHoles[temp - 9].OnHighlight();
                    yield return new WaitForSeconds(0.9f);
                    theirHoles[temp - 9].OnHighlightEnded();
                    yield return new WaitForSeconds(0.1f);
                }

            }
            else if (parameters.Length == 1)
            {
                yield return "sendtochaterror Please specify the position of the hole you wish to choose!";
            }
            yield break;
        }
    }

	IEnumerator TwitchHandleForcedSolve()
	{
		yield return null;
		yourHoles[Array.IndexOf(mostSeeds, mostSeeds.Max())].OnInteract();
	}
}