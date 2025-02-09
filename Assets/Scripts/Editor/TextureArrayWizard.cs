using UnityEditor;
using UnityEngine;

public class TextureArrayWizard : ScriptableWizard
{
	public Texture2D[] textures;

    [MenuItem("Assets/Create/Texture Array")]
    static void CreateWizard () {
		ScriptableWizard.DisplayWizard<TextureArrayWizard>(
			"Create Texture Array", "Create"
		);
	}
}