# SolidarityBookCatalog
以下均为天马行空的想法而已，请勿苛求！
一、联盟的纸质图书
1、基于MONGODB，建立一个图书馆联盟的书目记录中央库；
2、收集各馆馆藏：
	纸质图书条码命名规则：
		yangtzeu/b.isbn.978-7-302-48512-4.2018.16102
		信息有高校的域名.图书的ISBN和出版年，以及最后一位是对应的本馆书目记录号
		对应的本地查询和记录关联： https://calis.yangtzeu.edu.cn/opac/book/16102
   数字图书的规则为：
	cxstar/b.isbn.978-7-121-38380-9.2020.29f1350700036aXXXX
	jd/b.isbn.978-7-121-38380-9.2020.30607701
	keledge/b.isbn.978-7-121-38380-9.2020.704089657264050176
	最后一位为对应平台的唯一ID
	https://cxstar.com/Book/Detail?ruid=29f1350700036aXXXX
	https://gx.jd.com/ebook/30607701
	https://www.keledge.com/wrap/details/book?id=704089657264050176

3、实现ELASTICSEARCH的检索平台，聚合出版社，出版年，馆藏地
4、基于书目查询开展市内纸质图书互借（基于公交车站的众筹模式运送）
5、参考reshare探索给出版社付费、可控借阅（墨水屏）和过程公开透明（上区块链）的模式