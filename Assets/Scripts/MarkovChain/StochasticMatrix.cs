using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class StochasticMatrix {

	////////////////////////////////
	#region Bookkeeping

	// maps from a :-delimited string of past notes to a list
	// of observed frequencies for following notes
	// Size of this list is 1+numToStates (number of output options)
	// index numToStates is used to hold the sum,
	// all other indices are simply counts of how many times the 
	// from-note sequence was followed by that index.
	// Pretty sparse but nesting Dictionaries feels like overkill.
	private Dictionary<string,List<float>> data;

	private int numOutputStates = 0;

	private StringBuilder builder = new StringBuilder();

	#endregion Bookkeeping
	////////////////////////////////

	public StochasticMatrix(int numToStates)
	{
		data = new Dictionary<string, List<float>>();
		numOutputStates = numToStates;
	}

	public void incrementTransition(List<int> fromList, int toState, float incrementBy)
	{
		List<float> toList;

		string fromString = getPastString(fromList);

		if(! data.TryGetValue(fromString, out toList))
		{
			toList = new List<float>();
			for(int i=0; i<numOutputStates+1; i++)
			{
				toList.Add(0.0f);
			}
		}
		toList[toState] += incrementBy;
		toList[toList.Count-1] += incrementBy;
	}

	// Returning -1 feels bad, here. Probably better to throw some exceptiony thing.
	public int getSampleNote(List<int> fromList)
	{
		List<float> toList;

		string fromString = getPastString(fromList);

		if(data.TryGetValue(fromString, out toList))
		{
			float sum = 0.0f;
			float randVal = Random.Range(0, toList[toList.Count-1]);
			for(int i=0; i<toList.Count-1; i++)
			{
				sum += toList[i];
				if(randVal < sum)
				{
					return i;
				}
			}
		}

		return -1;
	}

	// Produce a string like 37:60:59:22s
	private string getPastString(List<int> pastStates)
	{
		builder.Remove(0, builder.Length);
		if(pastStates.Count == 0)
		{
			return "";
		}
		builder.Append(pastStates[0].ToString());
		for(int i=1; i<pastStates.Count; i++)
		{
			builder.Append(":"+pastStates[i]);
		}
		return builder.ToString();
	}
}
