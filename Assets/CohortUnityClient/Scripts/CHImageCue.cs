// Copyright Jacob Niedzwiecki, 2020
// Released under the MIT License (see /LICENSE)

using UnityEngine;

namespace Cohort
{
	[System.Serializable]
	public class CHImageCue
	{
		[SerializeField]
		public Sprite image;

		[SerializeField]
		public float cueNumber;

    [SerializeField]
    public string accessibleAlternative;

    public CHImageCue() { }

		public CHImageCue(Sprite sourceImage)
		{
			image = sourceImage;
		}
	}
}
