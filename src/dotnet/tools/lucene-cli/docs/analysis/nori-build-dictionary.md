# kuromoji-build-dictionary

### Name

`analysis-nori-build-dictionary` - Generates a set of custom dictionary files for the Lucene.Net.Analysis.Nori library.

### Synopsis

<code>lucene analysis nori-build-dictionary \<INPUT_DIRECTORY> \<OUTPUT_DIRECTORY> [-e|--encoding] [-n|--normalize] [?|-h|--help]</code>

### Description

Generates the following set of binary files:

- CharacterDefinition.dat
- ConnectionCosts.dat
- TokenInfoDictionary$buffer.dat
- TokenInfoDictionary$fst.dat
- TokenInfoDictionary$posDict.dat
- TokenInfoDictionary$targetMap.dat
- UnknownDictionary$buffer.dat
- UnknownDictionary$posDict.dat
- UnknownDictionary$targetMap.dat

If these files are placed into a subdirectory of your application named `nori-data`, they will be used automatically by Lucene.Net.Analysis.Nori features such as the KoreanAnalyzer or KoreanTokenizer. To use an alternate directory location, put the path in an environment variable named `nori.data.dir`. The files must be placed in a subdirectory of this location named `nori-data`.

See the wiki post (https://github.com/jimczi/nori/blob/14579127f7e8e4316f4dbfad64601c31d746d652/how-to-custom-dict.asciidoc#how-to-use-a-custom-dictionary-in-nori) for information about the dictionary format and how to customize it. Note that the instructions are for Java Lucene and Solr which only makes them useful for acquiring and modifying the data. For .NET it is generally best not to recompile Lucene.NET, but to use this tool to generate the data files and then place them into a subdirectory of your application named `nori-data`.

### Arguments

`INPUT_DIRECTORY`

The directory where the dictionary input files are located.

`OUTPUT_DIRECTORY`

The directory to put the dictionary output.

### Options

`?|-h|--help`

Prints out a short help for the command.

`-e|--encoding <ENCODING>`

The file encoding used by the input files. If not supplied, the default value is `utf-8`.

`-n|--normalize`

Normalize the entries using normalization form KC.

### Example

<code>lucene analysis nori-build-dictionary X:\kuromoji-data X:\kuromoji-dictionary --normalize</code>

