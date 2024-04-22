/*===============================================================================
Copyright (C) 2020 ARWAY Ltd. All Rights Reserved.

This file is part of ARwayKit AR SDK

The ARwayKit SDK cannot be copied, distributed, or made available to
third-parties for commercial purposes without written permission of ARWAY Ltd.

===============================================================================*/
#if UNITY_EDITOR

using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;

#if UNITY_2020_1_OR_NEWER
using UnityEditor.AssetImporters;
#else
using UnityEditor.Experimental.AssetImporters;
#endif

[ScriptedImporter(1, "ply")]
public class PLYImporter : ScriptedImporter
{
	private string VertexColorPath = "Assets/PLYImporter/Materials/VertexColor.mat";
	private GameObject pointCloud;

	private int numPoints;
	private int numPointGroups;
	private int limitPoints = 65000;

	private Vector3[] points;
	private Color[] colors;
	private Vector3 minValue;

	string assetName;

	// Send asset path to CreatePCD() function
	public override void OnImportAsset(AssetImportContext ctx)
	{
		CreatePCD(ctx.assetPath);
	}

	// Create Point Cloud from the given PLY file
	public void CreatePCD(string assetPath)
	{
		// Get Directory Path
		string dirName = Path.GetDirectoryName(assetPath) + "/";

		// Get File Name
		assetName = Path.GetFileNameWithoutExtension(assetPath);

		// Instantiate Points
		InstantiatePoints(assetName, dirName);
	}

	// Instantiates points under Point Cloud gameobject by getting the Vector3 values line by line
	public void InstantiatePoints(string assetName, string dirName)
	{
		pointCloud = new GameObject(assetName);
		string fileName = dirName + assetName + ".ply";
		Debug.Log("fileName" + fileName);

		// Get total number of points in the file
		string[] lines = File.ReadAllLines(fileName);
		string _points = lines[4];

		_points = Regex.Replace(_points, "[^0-9]+", string.Empty);
		int totalPoints = Convert.ToInt32(_points);
		numPoints = totalPoints - 21;

		Debug.Log("numPoints: " + numPoints);
		minValue = new Vector3();

		if (totalPoints > lines.Length)
		{
			totalPoints = lines.Length - 1;
		}

		points = new Vector3[numPoints];
		colors = new Color[numPoints];
		int[] indices = new int[totalPoints - 21];

		// Instantiate points
		for (int i = 21; i < totalPoints; i++)
		{
			points[i - 21] = GetXYZValue(lines[i - 1]);
			colors[i - 21] = GetColorValue(lines[i - 1]);
			indices[i - 21] = i - 21;
		}

		numPointGroups = Mathf.CeilToInt(numPoints * 1.0f / limitPoints * 1.0f);

		for (int i = 0; i < numPointGroups - 1; i++)
		{
			InstantiateMesh(assetName, i, limitPoints);
			if (i % 10 == 0)
			{
				string statusText = i.ToString() + " out of " + numPointGroups.ToString() + " PointGroups loaded";
				Debug.Log("" + statusText);
			}
		}
		InstantiateMesh(assetName, numPointGroups - 1, numPoints - (numPointGroups - 1) * limitPoints);
	}

	// Returns Vector3 coordinates of a given line
	public Vector3 GetXYZValue(string data)
	{
		char[] seperators = { ',', ' ' };

		String[] strlist = data.Split(seperators, StringSplitOptions.None);

		float x_val = float.Parse(strlist[0]);
		float y_val = float.Parse(strlist[1]);
		float z_val = float.Parse(strlist[2]);

		Vector3 xyz = new Vector3(x_val, y_val, z_val);

		return xyz;
	}

	// Returns Color of a given line
	public Color GetColorValue(string data)
	{
		char[] seperators = { ',', ' ' };

		String[] strlist = data.Split(seperators, StringSplitOptions.None);

		float r = float.Parse(strlist[3]);
		float g = float.Parse(strlist[4]);
		float b = float.Parse(strlist[5]);

		Color color = new Color(r / 255.0f, g / 255.0f, b / 255.0f);

		return color;
	}

	void InstantiateMesh(string filename, int meshInd, int nPoints)
	{
		// Create Mesh
		GameObject pointGroup = new GameObject(filename + meshInd);
		pointGroup.AddComponent<MeshFilter>();
		pointGroup.AddComponent<MeshRenderer>();
		pointGroup.GetComponent<Renderer>().material = AssetDatabase.LoadAssetAtPath<Material>(VertexColorPath);

		pointGroup.GetComponent<MeshFilter>().mesh = CreateMesh(meshInd, nPoints, limitPoints);
		pointGroup.transform.parent = pointCloud.transform;
	}

	Mesh CreateMesh(int id, int nPoints, int limitPoints)
	{

		Mesh mesh = new Mesh();

		Vector3[] myPoints = new Vector3[nPoints];
		int[] indecies = new int[nPoints];
		Color[] myColors = new Color[nPoints];

		for (int i = 0; i < nPoints; ++i)
		{
			myPoints[i] = points[id * limitPoints + i] - minValue;
			indecies[i] = i;
			myColors[i] = colors[id * limitPoints + i];
		}

		mesh.vertices = myPoints;
		mesh.colors = myColors;
		mesh.SetIndices(indecies, MeshTopology.Points, 0);
		mesh.uv = new Vector2[nPoints];
		mesh.normals = new Vector3[nPoints];
		return mesh;
	}
}
#endif