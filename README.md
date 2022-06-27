# Usage With Unity Instructions
Have the actual Unity project in a separate folder, and when you are done making changes, use the following command to copy over the correct files into the clone of this repo:
```
xcopy "path to your Unity project" "path to git clone" /S /XS
```
Put the file paths to your project and git clone in quotation marks and then only the files necessary for the git project will be copied over.
If you are pulling a change from the git project, just copy the git folder's contents (exluding .git and .gitattributes.txt) into the Unity project.
