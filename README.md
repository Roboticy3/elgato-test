# Usage With Unity Instructions
Have the actual Unity project in a separate folder, and when you are done making changes, use the following command to copy over the correct files into the clone of this repo:
```
xcopy "path to your Unity project" "path to git clone" /S /XS
```
I need to use command prompt in Administrator mode for this to work, try that if it doesn't work for you.
When the command prompt asks you to replace files (Yes/No/ALL), type A for it to replace all of them.

Put the file paths to your project and git clone in quotation marks and then only the files necessary for the git project will be copied over.
If you are pulling a change from the git project, just copy the git folder's contents (exluding .git, .gitattributes.txt, and this README file) into the Unity project.
