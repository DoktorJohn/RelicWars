⚔️ RelicWars - Development Guide
This repository contains both the C# Backend (ASP.NET Core) and the Unity Game Client. To ensure smooth collaboration and prevent data loss, all developers must follow this guide.

🛠 1. Prerequisites (Install these first)
Before touching the code, ensure you have the following tools installed:

Git for Windows: Download here. https://git-scm.com/install/windows

Git LFS (Large File Storage): CRITICAL! Download here. https://git-lfs.com/ Unity's 3D models and high-res textures require this.

Unity Hub & Unity 2022.3+: Ensure you have the correct version installed.

Visual Studio 2022: With the "Game development with Unity" and ".NET desktop development" workloads selected.

🚀 2. Initialization (First-time setup)
Once the software is installed, follow these steps to download the project:

Right-click in the folder where you want the project and select Git Bash Here (or open CMD).

Enable LFS on your machine:

Bash

git lfs install
Clone the repository:

Bash

git clone https://github.com/DoktorJohn/RelicWars.git
Enter the project folder:

Bash

cd RelicWars
🌿 3. Choosing Your Branch
We never work directly on the main branch. You have your own dedicated branch for development:

Download all the latest branch names:

Bash

git fetch
Switch to your own branch (Replace name with your actual branch name):

Bash

git checkout development/feature-implementation-name
🔄 4. Daily Workflow (Follow this strictly)
To avoid "Merge Conflicts" and losing work, always follow this sequence:

Start of the Day:
Get your partner's latest merged changes into your branch:

Bash

git pull origin main
During / End of the Day:
When you have made changes, save them and send them to GitHub:

Stage your changes:

Bash

git add .
Create a "Save Point" (Commit):

Bash

git commit -m "Describe what you did (e.g., Added Warehouse model and logic)"
Send to GitHub:

Bash

git push
⚠️ 5. Golden Rules for Unity & Git
NEVER delete .meta files: Unity uses these to track your assets. If you delete them, the project will break for everyone else.

Save Scenes before committing: Ensure you have saved your work in the Unity Editor before running git add ..

Fixing Pink Models / Missing Files: If 3D models look wrong or are missing, run the command: git lfs pull.

Mono-repo Care: Remember that your changes might affect the /game (Backend) and /gameUnity (Client) folders. Keep them synchronized!

📂 Folder Structure
/game: C# WebApi, Application, and Domain logic (Backend).

/gameUnity: The actual Unity project files (Client).
