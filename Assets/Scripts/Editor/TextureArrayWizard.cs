using UnityEditor;
using UnityEngine;

public class TextureArrayWizard : ScriptableWizard
{
    [MenuItem("Assets/Create/Texture Array")]
    static void CreateWizard () {
		ScriptableWizard.DisplayWizard<TextureArrayWizard>(
			"Create Texture Array", "Create"
		);
	}
}