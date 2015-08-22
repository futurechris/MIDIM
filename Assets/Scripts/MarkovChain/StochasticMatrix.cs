using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StochasticMatrix {

	////////////////////////////////
	#region Bookkeeping

	// right stochastic matrix, so rows add to 1.0
	// probMatrix[row][col]
	private float[,] probMatrix;

	// once a change has been made, the matrix rows are unlikely
	// to still sum to 1. this keeps track of that, and is used
	// to ensure normalization occurs before any requests are served
	//
	// obviously you may want to make that happen manually - normalization
	// order matters, and can be expensive!
	private bool normalized = false;

	#endregion Bookkeeping
	////////////////////////////////

	public StochasticMatrix(int numStates)
	{
		probMatrix = new float[numStates][numStates];
		normalized = true;
	}

	public void setProbability(int fromState, int toState, float newProbability)
	{
		normalized = false;
		probMatrix[fromState, toState] = newProbability;
	}

	public void normalizeRows()
	{
		int rowBound = probMatrix.GetUpperBound(0);
		int colBound = probMatrix.GetUpperBound(1);
		for(int row=0; row<rowBound; row++)
		{
			float sum = 0.0f;
			float secondSum = 0.0f;
			for(int col=0; col<colBound; col++)
			{
				sum += probMatrix[row,col];
			}
			if(sum == 1.0f)
			{
				continue;
			}
			for(int col=0; col<colBound; col++)
			{
				probMatrix[row,col] /= sum;
				secondSum += probMatrix[row, col];
			}
			probMatrix[row, colBound-1] += (1.0-secondSum); // maybe solve float precision error? :)
		}
		normalized = true;
	}
}
