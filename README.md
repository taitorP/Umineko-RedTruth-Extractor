# Red truth extractor

## The red only tells the truth

First exercise with web scraping, coding a way to extract the so called **Red Truths**, a fundamental piece of the narrative of the visual novel _Umineko no naku koro ni_ (Umineko when they cry), from the dedicated [page](https://wiki.whentheycry.org/wiki/Red_Truth) from the When They Cry Wiki.

This tool asks for an episode from the visual novel the user wants to know the red truths of, it makes a request through the MediaWiki API for the list of all sections of the page Red Truth and checks for the section with the corresponing name. If the request is successful, the console prints the list of all red truths.

## Knox's 8th. It is forbidden for the case to be resolved with clues that are not PRESENTED!!

- This project was made in **C# 13** and **.NET 10**
- **System.Text.Json** is used to manage API responses
- **HtmlAgilityPack** is used to parse the content received, navigate through it and clean it of all the tags and the links that were not needed

## There were no tricks like that!! It was just an ordinary table and an ordinary cup!!

1. Start the application
2. Enter the name or the number of the episode you want to know the red truths of
3. Read the red truths as they are printed out on the console

![Example of usage](images/red_truth.png)
