# DotOekaki（ドットお絵かきゲーム）

## 概要

オンラインで最大６人まで遊べる、リアルタイム描画同期のドット絵お絵かきゲームです。
一人のプレイヤーが絵を描き、他のプレイヤーが当てるクイズ形式を中心に、4種類のゲームモードを実装しています。
自分が描いたドット絵をPNGまたはJPEG画像としてダウンロードできます。

---

## プレイURL

https://unityroom.com/games/dot_oekaki

---

## 主な機能

* お絵かきクイズ（1人が描いて他プレイヤーが回答）
* 協力お絵かきクイズ（2人で同時に描いて他プレイヤーが回答）
* 絵しりとり（最初の文字と最後の文字がつながるようにドット絵でお絵かき）
* 伝言ゲーム（お題に沿ってドット絵で伝言ゲーム）
* 一人用お絵かき（描いたドット絵をpngまたはjpegでダウンロード可能）

---

## 使用技術

* Unity（C#）
* Photon PUN2（リアルタイム通信）
* Texture2D を用いたドット描画システム

---

## 工夫した点

* ピクセル単位での描画処理（フリーハンド、直線ツール、円・四角ツール、塗りつぶしツール）、Undo / Redo 機能を実装
* リアルタイムでの描画同期（Photon）
* 描画キャンバスのピクセルサイズを変更できるように設計し、プレイヤーのプレイの幅を広げてゲームに深みを持たせた
* スプレッドシートと連携させて、クイズモード等のお題リストを外部管理

---

## スクリーンショット

<img width="627" height="346" alt="image" src="https://github.com/user-attachments/assets/095713cd-b7a2-49d8-950b-12ddbd23278d" />
<img width="627" height="351" alt="image" src="https://github.com/user-attachments/assets/cf5ffc69-0d1c-4296-bf99-1628db257c6a" />
<img width="624" height="351" alt="image" src="https://github.com/user-attachments/assets/fdb35797-8ea6-4d7b-ac81-5388d069e15e" />
<img width="620" height="348" alt="image" src="https://github.com/user-attachments/assets/91442aa9-2c95-4277-a18f-e7bad1a5efe7" />

---

## 開発補助ツール

本プロジェクトでは開発の一部において ChatGPT を活用しています。

主に以下の用途で使用しました：
・コードの改善案の検討
・不具合調査の補助
・設計に関するアイデア整理

最終的な設計および実装は自身で行っています。

---

## クレジット（ゲーム内に詳細を記載）

* BGM：DOVA-SYNDROME
* 効果音：イワシロ音楽素材
* フォント：DotGothic16（Google Fonts）
