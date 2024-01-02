[link]: https://github.com/eliasailenei/MSWISO/releases/tag/Release
[site]: https://files.rg-adguard.net
[aria]: https://aria2.github.io/
[fid]: https://github.com/pbatard/Fido
[paypal]: https://paypal.me/eliasppl
# MSWISO

## A complex yet easy solution to get your Windows images from a geniune source

#### This C# program is handy tool that allows you to get your Windows Version without the hassle of using Windows Media Creation Tool or other alternatives. This program scrapes [files.rg-adguard.net][site] for the latest releases of Windows direct links and makes them more accessible.

##### Some features include
* Many Windows versions and releases
* ESD only downloads
* Other languages
* Archived versions of Windows

# How it works and to get started
![image](https://github.com/eliasailenei/MSWISO/assets/82527761/848f88ce-05b6-4759-ad60-bb12992e1851)

## ⚠️ .NET 4.8 IS REQUIRED ⚠️

##### Note, this program only works with command-line arguments e.g., MSWISO.exe --WinVer=Windows_10 --Release=

#### To get started, you just need to get the ZIP file from releases [which you can find here.][link]

#### Then you can use the arguments as seen in the screenshot, here is the structure if you still need help.

* State by saying if you are only looking for ESD --> --ESDMode=True
* Start by choosing the version you want --> --WinVer=Windows_10 |We use _ to show spaces|
* Then by the release --> --Release |Leave blank if you want to generate the list, don't forget to add underscore!|
* Followed by a language --> --Release |Leave blank if you want to generate the list, don't forget to add underscore!|
##### You can also specify a location with --Location=

# Bugs and support
### If you have found any bugs, please report it on the repo or branch a fix |if you'r cool 😎|.
#### Many thanks to [aria2c][aria] for being a good option to get the torrents downloaded.
### And also [files.rg-adguard.net][site], without this site, I would probably fail my project 😅.
##### Also thanks to [Fido][fid] for giving me the idea.
#### You can also [support me by donating too][paypal].



