namespace VRainPdf;

public class BookConfig
{
    public string? Title { get; set; }
    public string? Author { get; set; }

    public string CanvasId { get; set; } = "bamboo"; //古籍刻本背景图ID
    public int RowNumber { get; set; } = 24; //每列字数
    public int RowDeltaY { get; set; } = 10; //列最后字符到边框距离

    //字体
    public string Font1 { get; set; } = "qiji-combo.ttf";
    public string Font2 { get; set; } = "HanaMinA.ttf";
    public string Font3 { get; set; } = "HanaMinB.ttf";
    public string Font4 { get; set; } = string.Empty;

    public int TrySt { get; set; } = 0; //不建议开启！字体不支持时尝试繁简、简繁转换，也许会改善字体支持情况，但很可能出现语境不符

    public double Font1Rotate { get; set; } = 0; //字体旋转角度
    public double Font2Rotate { get; set; } = 0;
    public double Font3Rotate { get; set; } = 0;
    public double Font4Rotate { get; set; } = 0;

    //正文字体大小、颜色
    public double TextFont1Size { get; set; } = 65;
    public double TextFont2Size { get; set; } = 60;
    public double TextFont3Size { get; set; } = 60;
    public double TextFont4Size { get; set; } = 60;
    public string TextFontColor { get; set; } = "black";

    //批注字体大小、颜色
    public double CommentFont1Size { get; set; } = 30;
    public double CommentFont2Size { get; set; } = 30;
    public double CommentFont3Size { get; set; } = 30;
    public double CommentFont4Size { get; set; } = 30;
    public string CommentFontColor { get; set; } = "black";

    //封面标题字体大小、颜色、高度
    public double CoverTitleFontSize { get; set; } = 120;
    public int CoverTitleY { get; set; } = 200;
    public double CoverAuthorFontSize { get; set; } = 60;
    public int CoverAuthorY { get; set; } = 600;
    public string CoverFontColor { get; set; } = "black";


    //版心标题字体大小、颜色、高度、字间距比例
    public bool IfTpCenter { get; set; } = false; //版心标题页码是否居中，1时居中，0是居左侧
    public double TitleFontSize { get; set; } = 50;
    public string TitleFontColor { get; set; } = "black";
    public int TitleY { get; set; } = 800;
    public double TitleYDis { get; set; } = 1.2;

    public string?
        TitilePostfix { get; set; } //版心标题后缀，X会自动替换，若存在保存前言、序的000.txt文件，将自动更新为序，若存在保存后记、附录的999.txt文件，将自动更新为附，不需要后缀时置空即可

    public bool TitleDirectory { get; set; } = true; //根据标题自动添加PDF目录

    //版心页码字体大小、颜色、高度
    public double PagerFontSize { get; set; } = 30;
    public string PagerFontColor { get; set; } = "black";
    public int PagerY { get; set; } = 400;

    //标点符号处理规则，顺序：替换->删除->模式（有标点，无标点，归一化）
    public string ExpReplaceComma { get; set; } = ",，|.。|:：|;；|!！|?？|(（|)）|（〔|）〕|{〔|}〕|<〔|>〕|[〔|]〕|“「|”」|‘『|’』|⋯…";
    public string ExpReplaceNumber { get; set; } = "1一|2二|3三|4四|5五|6六|7七|8八|9九|0〇|１一|２二|３三|４四|５五|６六|７七|８八|９九|０〇";
    public string ExpDeleteComma { get; set; } = "．|　|-|─||〖|〗"; //删除的标点符号，以|分隔

    public int IfNocomma { get; set; } = 1; //无标点符号模式
    public string ExpNocomma { get; set; } = "、|，|。|：|；|！|？|〔|〕|「|」|『|』"; //无标点符号模式下过滤的标点符号, 以|分隔，if_nocomma为1时有效
    public int IfOnlyperiod { get; set; } = 0; //标点符号归一化为句号
    public string ExpOnlyperiod { get; set; } = "、|，|。|：|；|！|？|〔|〕|「|」|『|』"; //归一化为句号的标点符号，以|分隔，if_onlyperiod为1时有效

    //正文标点符号
    public string TextCommaNop { get; set; } = "、|，|。|：|；|！|？"; //不占独立字符位置的标点符号
    public double TextCommaNopSize { get; set; } = 0.6; //不占独立字符位置标点符号大小缩放
    public double TextCommaNopX { get; set; } = 0.7; //不占独立字符位置标点符号横向位置调整，越大越往右移
    public double TextCommaNopY { get; set; } = 0.2; //不占独立字符位置标点符号纵向位置调整，越大越往下移
    public string TextComma90 { get; set; } = "「」『』〔〕…"; //旋转90度的标点符号
    public double TextComma90Size { get; set; } = 0.8; //旋转90度标点符号大小缩放
    public double TextComma90X { get; set; } = 0.5; //旋转90度标点符号横向位置调整，越大越往右移
    public double TextComma90Y { get; set; } = 0.6; //旋转90度标点符号纵向位置调整，越大越往上移

    //批注标点符号
    public string CommentCommaNop { get; set; } = "、|，|。|：|；|！|？";
    public double CommentCommaNopSize { get; set; } = 0.7;
    public double CommentCommaNopX { get; set; } = 0.65;
    public double CommentCommaNopY { get; set; } = 0.3;
    public string CommentComma90 { get; set; } = "「」『』〔〕…";
    public double CommentComma90Size { get; set; } = 0.8;
    public double CommentComma90X { get; set; } = 0.15;

    public int IfBookVline { get; set; } = 0; //将书名号《》转换为侧重点线
    public double BookLineWidth { get; set; } = 1; //侧线宽度
    public string BookLineColor { get; set; } = "black"; //侧线颜色

    //全局标记符号，修改无效
    public string TagComment { get; set; } = "【】"; //标识批注文字
    public string TagNewpage { get; set; } = "%"; //分页符号
    public string TagHalfpage { get; set; } = "$"; //半页分页符号
    public string TagLastcol { get; set; } = "&"; //跳至本页最后一列，用于卷回文本末行文字
    public string TagBookilne { get; set; } = "《》"; //书名号转换为字符侧边线
    public string TagSpace { get; set; } = "@"; //代表空格
}