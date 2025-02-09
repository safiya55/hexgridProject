using UnityEditor;
using UnityEngine;

public class TextureArrayWizard : ScriptableWizard
{
	public Texture2D[] textures;

	[MenuItem("Assets/Create/Texture Array")]
	static void CreateWizard()
	{
		ScriptableWizard.DisplayWizard<TextureArrayWizard>(
			"Create Texture Array", "Create"
		);
	}

	//method that gets invoked when the create button is pressed
	void OnWizardCreate()
	{
		//where we create texture array if user has added any texture to the wizard
		if (textures.Length == 0)
		{
			//abort if none is created
			return;
		}

		//ask the user where to save the texture array asset. 
		//parameters determine the panel name, default file name, the file extension, and description.
		//Texture arrays use the generic asset file extension
		string path = EditorUtility.SaveFilePanelInProject( //returns the file path that the user selected.
			"Save Texture Array", "Texture Array", "asset", "Save Texture Array"
		);

		//f the user canceled the panel, then the path will be the empty string.
		if (path.Length == 0)
		{
			//abort
			return;
		}

		//when have valid path
		Texture2D t = textures[0];
		Texture2DArray textureArray = new Texture2DArray(
			t.width, t.height, textures.Length, t.format, t.mipmapCount > 1
		);

		textureArray.anisoLevel = t.anisoLevel;
		textureArray.filterMode = t.filterMode;
		textureArray.wrapMode = t.wrapMode;

		for (int i = 0; i < textures.Length; i++) {
			for (int m = 0; m < t.mipmapCount; m++) {
				Graphics.CopyTexture(textures[i], 0, m, textureArray, i, m);
			}
		}

		AssetDatabase.CreateAsset(textureArray, path);
	}

	
}