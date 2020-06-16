using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ModelColourEditor
{
	public static class ColorExtensions
	{
		public static Color ToAlpha(this Color color, float a)
		{
			color.a = a;
			return color;
		}
	}
}