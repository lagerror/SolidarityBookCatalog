# SolidarityBookCatalog
以下均为天马行空的想法而已，欢迎同志们加入，请勿苛求！
一、联盟的纸质图书
1、基于MONGODB，建立一个图书馆联盟的书目记录中央库；
	图书以ISBN，统一规范位13位，作为唯一索引（虽然不合理，但是在当前没有权威的，公开的，免费的书目库提供唯一标识用来查重的情况下不得已为之，
	并且将书目控制在出版时间为2000年以后的图书）
	提供API接口，用于各馆提交自己的书目记录，加密方式为sign=md5(md5(appid+nonce(时间戳)+isbn)+appkey),为什么选择MD5而不是sha256等，因为考虑
	到某些情况下MD5更容易实现
	书目的成量提交：
	书目的增量提交：
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
3、收集脱敏的流通记录
4、实现ELASTICSEARCH的检索平台，聚合出版社，出版年，馆藏地
5、基于书目查询开展市内纸质图书互借（基于公交车站的众筹模式运送）
6、参考reshare探索给出版社付费、可控借阅（墨水屏）和过程公开透明（上区块链）的模式


二、联盟的活动
1、数字图书平台的推荐购买，组队推广一个小专题的六本图书，通过海报，视频和新媒体推广，后台统计图书的阅读记录，综合权重评分

三、联盟的开放项目
