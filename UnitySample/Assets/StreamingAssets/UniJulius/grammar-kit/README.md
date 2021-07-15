# Recognition Grammar Toolkit for Julius

This package is a grammar kit for Julius, containing:

- How-to documents to use and build grammar,
- Sample grammars (ja/en),
- Julius executables for Win/Linux,
- Conversion tools,
- Acoustic models (ja).

For English, an English acoustic model is needed to run the sample grammars on Julius.  Sample grammars cannot be run without English acoustic model for Julius.  Currently we have no English acoustic model available for free.  Sorry for inconvenience.

For Japanese, you can run recognition on the sample grammars with this kit. first take a look at [00readme-ja.txt](https://github.com/julius-speech/grammar-kit/blob/master/00readme-ja.txt) and [HOWTO-ja.txt](https://github.com/julius-speech/grammar-kit/blob/master/HOWTO-ja.txt) to see how to run the samples in Japanese.  After that, read the instruction below to build your own grammar.


# How to write a recognition grammar for Julius

## Recognition Grammar

[Julius](https://github.com/julius-speech/julius) can perform speech recognition based on a written grammar. A grammar describes possible syntax or patterns of words on a specific task. When given a speech input, Julius searches for the most likely word sequence under constraint of the given grammar.

The following is a brief description of how to write a recognition grammar for Julius.

## Write a recognition grammar for Julius

In Julius, the recognition grammar should be given into two separate files:
- ".grammar" file
- ".voca" file

The ".grammar" file defines category-level syntax, i.e. allowed connection of words by their category name. The ".voca" file is a dictionary that defines word candidates in each category, with its pronunciation information.

Examples of the grammar are included in this grammar kit.

### .grammar file

The allowed connection of words should be defined in ".grammar" file, using word category names as terminal symbols.

Below is an example grammar file for the fruit order task, "fruit.grammar". It's like a BNF style. The initial sentence symbol to start should be "S". The phrase rules should be defined each per line, using ":" as delimiter. Characters of ascii alphabets, numbers, and underscore are allowed for the symbol names, and they are case sensitive.

```
S : NS_B HMM SENT NS_E
S : NS_B SENT NS_E
SENT: TAKE_V FRUIT PLEASE
SENT: TAKE_V FRUIT
SENT: FRUIT PLEASE
SENT: FRUIT
FRUIT: NUM FRUIT_N
FRUIT: FRUIT_N_1
```

Julius assumes that each word in dictionary belongs to a "word category", and the grammar should be written in the category level.  Thus, the terminal symbols in grammar, i.e. symbols which does not appear on the left side, must correspond to the word category defined in the .voca file.

In this example, (`NS_B`, `NS_E`, `HMM`, `TAKE_V`, `PLEASE`, `NUM`, `FRUIT_N`, `FRUIT_N_1` are terminal symbols (i.e. word categories), and their belonging words should be defined in .voca file. `NS_B` and `NS_E` corresponds to the head silence and tail silence of an input speech, and should be defined in all grammar for Julius since most speech input assumes certain length of silence on head and tail.

If you want to use an "infinite loop" in part of your grammar, you should write a recursion rule like this (only left-recursion is allowed):

```
S: NS_B WORD_LOOP NS_E
WORD_LOOP: WORD_LOOP WORD
WORD_LOOP: WORD
```

Although this BNF-like writing allows up to CFG class, Julius can handle only regular expression class, since Julius uses DFA parser. If you write a grammar whose class goes over the DFA class, the grammar compiler (will be explained below) will complains it.

### .voca file

.voca file contains word definition for each word category defined in the .grammar file. Below is the corresponding "fruit.voca" file.

```
% NS_B
<s>		sil

% NS_E
</s>		sil

% HMM
FILLER		f m
FILLER		w eh l

% TAKE_V
I'lltake	ay l t ey k
I'llhave	ay l hh ar v

% PLEASE
please		p l iy z

% FRUIT_N_1
apple		ae p ax l
orange		ao r ax n jh
orange		ao r ix n jh
grape		g r ey p
banana		b ax n ae n ax
plum		p l ah m

% FRUIT_N
apples		ae p ax l z
oranges		ao r ax n jh ax z
oranges		ao r ix n jh ix z
grapes		g r ey p s
bananas		b ax n ae n ax z
plums		p l ah m s

% NUM
one		w ah n
two		t uw
three		th r iy
four		f ao r
five		f ay v
six		s ih k s
seven		s eh v ax n
eight		ey t
nine		n ay n
ten		t eh n
eleven		ix l eh v ax n
twelve		t w eh l v
```

After specifying a word category with "`%`", words in the category should be defined each by line. The first column is the string which will be output when recognized, and the rest are the pronunciation. Space and tab are the field separator. In the example above, `NS_B` and `NS_E` category has one word entry with silence model, to correspond with the head and tail silence in speech input.

The pronunciation should be defined as a sequence of HMM name in your acoustic model. If you have some variety in pronunciation of a word, you can define all the variations each by line. See the word entry "orange" above.

## Compile a grammar to Julius DFA format

.grammar and .voca files should be compiled into category DFA (.dfa) file and word dictionary (.dict) file to be used in Julius.  The grammar compiler is included in the Julius distribution, which can be invoked as "mkdfa.pl". To use it, specify the prefix of .grammar and .voca file. An example of running "mkdfa.pl" on the example grammar above is as followings

```
% mkdfa.pl fruit
fruit.grammar has 8 rules
fruit.voca    has 8 categories and 31 words
---
Now parsing grammar file
Now modifying grammar to minimize states[0]
Now parsing vocabulary file
Now making nondeterministic finite automaton[10/10]
Now making deterministic finite automaton[10/10]
Now making triplet list[10/10]
---
-rw-r--r--    1 foo      users         182 May  9 16:03 fruit.dfa
-rw-r--r--    1 foo      users         626 May  9 16:03 fruit.dict
-rw-r--r--    1 foo      users          66 May  9 16:03 fruit.term
```

The generated "fruit.dfa" contains finite automaton information, and "fruit.dict" contains word dictionary, both in Julius format.


## Generating sentences from grammar

One easy method to check the coverage of your grammar is to generate word sequences according to your grammar. Generation of allowed utterances will help knowing what utterance patterns are covered on that grammareasier than analizing the grammatical rules itself. You can check your grammar by looking whether invalid utterance are generated, or required utterances are not generated.

A tool `generate` is provided with Julius which generates random sentences from a grammar. You can also specify the number of sentence generation by `-n`, and also can select terminal name output instead of word instances by `-t` (.term file is needed).

Below is an example of executing `generate` on the example grammar `fruit`.

```
% generate fruit
Reading in dictionary...
31 words...done
Reading in DFA grammar...done
Mapping dict item <-> DFA terminal (category)...done
Reading in term file (optional)...done
8 categories, 31 words
DFA has 10 nodes and 18 arcs
-----
 <s> FILLER seven apples please </s>
 <s> banana </s>
 <s> FILLER I'llhave three oranges </s>
 <s> five bananas </s>
 <s> eight plums </s>
 <s> FILLER I'lltake seven plums </s>
 <s> FILLER banana </s>
 <s> ten oranges </s>
 <s> FILLER plum </s>
 <s> apple </s>
```

## Using grammar on Julius

*Warning: An English acoustic model is needed to run the recognition grammars on Julius.
Currently we have no English acoustic model available for free.  Descriptions below are mere explanation of grammar specification in Julius. 
Sorry for inconvenience.*

To use the grammar on Julius,

- Remove `-d ngram` and `-v dictionary` options from its configuration, since they are N-gram files
- Specify the grammar prefix by `-gram prefix` option, or specify separate files by `-dfa dfafile` and `-v dict`.

Julius supports multiple grammar recognition.  When specifying grammars mutilple times using `-gram`, Julius reads all of them and perform recognition for all the grammars, and output only result of the highest score.  You can also get recognition results for all the given grammars by specifying `-multigramout` option.

The rest options are the same as Julius. See other documents of Julius for all the detailed functions and options of Julius.

## Tools

### slf2dfa

This toolkit converts an HTK recognition grammar into Julian format. A word network (SLF) will be converted to DFA format, and the words in the SLF are extracted from the dictionary to be used in Julian. Furthermore, word category will be automatically detected and defined to optimize performance.

To use the tool, see the "tools/slf2dfa" folder.
