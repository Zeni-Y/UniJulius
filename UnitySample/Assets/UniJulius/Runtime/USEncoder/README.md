unicode-sjis-encoder
=================================

## 概要
Unicode(UTF-16)とSJIS(cp932)を相互に変換するためのライブラリです．
環境によってEncoding.GetEncoding(932)が使えない場合があるため作りました．

utf8という単語が出現することがありますが，実装上の諸事情（C#の文字リテラルがそもそもUnicode，UTF-8よりもUnicodeのほうが軽く作れる等）によりUnicodeになりました．


## 注意点
基本的にSJISが大前提の環境を想定して作っているため，
SJISに存在しない文字を投げると確実に文字化けします．
気になる場合は自前でエスケープしてください．

また，UnicodeからSJISに変換する場合は，
文字リテラルがUnicodeであることに注意してください．


## 使い方
ToEncoding.ToSJIS関数，ToEncoding.ToUTF8関数を用意しています．

### ToEncoding.ToSJIS(string unicode_str) : byte[]
UnicodeからSJISへ変換する関数です．
SJISへ変換済みのbyte型の配列を返します．

utf8_strはUnicodeに変換された文字列です．

### ToEncoding.ToUnicode(byte[] sjis_bytes) : string
SJISからUnicodeへ変換する関数です．
Unicodeへ変換済みの文字列型を返します．

sjis_bytesはSJISに変換されたbyte型配列です．


## Copyright
このライブラリはパブリック・ドメインです．
報告，著作権表記等は必要ありません．
自由にお使いください．


## 免責
本ライブラリは利用者の責任においてご利用ください．
竹渕瑛一は本ライブラリの利用により生じた利用者の損害について一切責任を負いません．
また，竹渕瑛一は本ライブラリの動作は一切保証致しません．


竹渕瑛一(GRGSIBERIA)
[Twitter](https://twitter.com/GRGSIBERIA)