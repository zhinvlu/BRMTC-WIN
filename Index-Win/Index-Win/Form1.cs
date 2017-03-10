using AForge.Neuro;
using AForge.Neuro.Learning;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Index_Win
{
    public partial class Index : Form
    {
        #region 宏
        const int OBJECTNUM = 48;
        const int IndexNUM = 10;//10个指标
        const int N = 100000;
        const int OBJECTNUM1 = 12;//12个省份
        const int LITTLE1 = 2;
        const int LITTLE2 = 3;
        const int YEAR = 4; //4年
        const int Input_layer = 10;
        const int Hidden_layer = 6;
        const int Output_layer = 1;
        const double Learning_Rate = 0.3;
        const double Momentum = 0.0;
        const bool needToStop = false;
        const int iterations = 10000;
        #endregion
        #region 成员变量
        List<double> holeindex = new List<double>();
        //List < double > outputall_2011 = new List<double>();
        double[][] input = new double[OBJECTNUM][];//神经网络input
        double[][] output = new double[OBJECTNUM][];//神经网络output
        List<List<double>> listGroup = new List<List<double>>();
        List<double> outputall = new List<double>();
        List<double> Zhi_Shu_List = new List<double>();
        List<List<double>> Zhi_Shu_List_four_year = new List<List<double>>();
        List<Dictionary<string, double>> Zhi_Shu_List_four_year_dic = new List<Dictionary<string, double>>();
        string line;
        BackPropagationLearning teacher;
        List<double> r_ij;
        List<double> R_ij;
        List<double> S_ij;
        Dictionary<string, double> Zhi_Shu_Zi_Dian;
        List<double> shang_list;
        List<double> quan_zhong_xi_shu_list;
        List<double> Ji_Chu_Huan_Jing_List;
        List<double> Tou_Ru__List;
        List<double> Chan_Chu_List;
        List<double> Ji_Xiao_List;
        List<Dictionary<string, Node>> Xiao_Zhi_Shu_List_four_year_dic = new List<Dictionary<string, Node>> ();
        List<List<Node>> Yangben_arg_four_year = new List<List<Node>>();
        ActivationNetwork myNetwork;
        int inputCount = 10;
        int[] neuronCount = new int[3] { 10, 6, 1 };
        double[,] trainData;
        double[] targetData;
        int trainCount = 48;
        #endregion

        public Index()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            #region 从txt中读取数据
            openFileDialog1.ShowDialog();
            StreamReader sr = new StreamReader(openFileDialog1.FileName, Encoding.Default);
            List<double> holeindex_all_year = new List<double>();
            for (int j = 0; j < OBJECTNUM * IndexNUM; ++j)
            {
                line = sr.ReadLine();
                holeindex_all_year.Add(Convert.ToDouble(line));
            }
            #endregion
            #region 计算大指数
            for (int j = 0; j < YEAR; j++)
            {
                holeindex = Split_All_Year_Data_To_Eachyear(holeindex_all_year, j);
                listGroup = list_fen_zu(holeindex, IndexNUM, OBJECTNUM1);
                outputall = Normalization(listGroup);
                listGroup = list_fen_zu(outputall, IndexNUM, OBJECTNUM1);
                Compute_Shang_List(listGroup, out shang_list);
                Quan_Zhong_xi_shu(shang_list, out quan_zhong_xi_shu_list);
                listGroup = list_fen_zu1(outputall, OBJECTNUM1);
                Zhi_Shu_List = Compute_ZhiShu_List(listGroup, quan_zhong_xi_shu_list);
                init_input_and_output_data(input, output, listGroup, Zhi_Shu_List, j);
            }            
            SearchSolution(out teacher, input, output);
            Compute_r_ij(out r_ij, teacher.weightsUpdates[0], teacher.weightsUpdates[1]);
            Compute_R_ij(out R_ij, r_ij);
            Compute_S_ij(out S_ij, R_ij);
            for (int i = 0; i < YEAR; i++)
            {
                holeindex = Split_All_Year_Data_To_Eachyear(holeindex_all_year, i);
                listGroup = list_fen_zu(holeindex, IndexNUM, OBJECTNUM1);
                outputall = Normalization(listGroup);
                listGroup = list_fen_zu(outputall, IndexNUM, OBJECTNUM1);
                listGroup = list_fen_zu1(outputall, OBJECTNUM1);
                Zhi_Shu_List = Compute_ZhiShu_List(listGroup, S_ij);
                init_dictionary(out Zhi_Shu_Zi_Dian, Zhi_Shu_List);
                Zhi_Shu_List_four_year_dic.Add(Zhi_Shu_Zi_Dian);
                //Zhi_Shu_List_four_year.Add(Zhi_Shu_List);
            }
            #endregion
            #region kmeans
            int year = 2011;
            for (int k = 0; k < YEAR; k++)
            {
                holeindex = Split_All_Year_Data_To_Eachyear(holeindex_all_year, k);
                listGroup = list_fen_zu(holeindex, IndexNUM, OBJECTNUM1);
                outputall = Normalization(listGroup);
                listGroup = list_fen_zu(outputall, IndexNUM, OBJECTNUM1);
                JI_CHU_HUAN_JING(listGroup, outputall, S_ij, year, out Zhi_Shu_Zi_Dian, out Ji_Chu_Huan_Jing_List);
                TOU_RU(listGroup, outputall, S_ij, year, out Zhi_Shu_Zi_Dian, out Tou_Ru__List);
                CHAN_CHU(listGroup, outputall, S_ij, year, out Zhi_Shu_Zi_Dian, out Chan_Chu_List);
                JI_XIAO(listGroup, outputall, S_ij, year, out Zhi_Shu_Zi_Dian, out Ji_Xiao_List);
                K_MEANS(Ji_Chu_Huan_Jing_List, Tou_Ru__List, Chan_Chu_List, Ji_Xiao_List);
                year++;
            }year = 2011;
            #endregion
        }

        private void K_MEANS(List<double> Ji_Chu_Huan_Jing_List, List<double> Tou_Ru__List, List<double> Chan_Chu_List, List<double> Ji_Xiao_List)
        {
            List<Node> Yangben_arg = new List<Node>();  //数组， 存放12个省份的样本
            //step1 定义样本变量（如陕西。云南等）
            List<Node> clusters = new List<Node>();         //数组，存放均值
            int numberOfClusters = 4;                      //分几组
            //初始化样本list
            for (int i = 0; i < OBJECTNUM1; i++)
            {
                Node tempnode = new Node();
                tempnode.Ji_Chu_Huan_Jing = Ji_Chu_Huan_Jing_List[i];
                tempnode.Tou_Ru = Tou_Ru__List[i];
                tempnode.Chan_chu = Chan_Chu_List[i];
                tempnode.Ji_xiao = Ji_Xiao_List[i];
                Yangben_arg.Add(tempnode);
            }
            //初始化均值list
            for (int i = 0; i < numberOfClusters; i++)
            {
                Node tempnode = new Node();
                clusters.Add(tempnode);
            }


            bool _changed = true;
            bool _success = true;
            // 初始化质心
            InitializeCentroids(Yangben_arg, numberOfClusters);

            int maxIteration = Yangben_arg.Count * 10;
            int _threshold = 0;
            while (_success == true && _changed == true && _threshold < maxIteration)
            {
                ++_threshold;
                _success = UpdateNodeMeans(Yangben_arg, clusters);  //更新均值（Means）
                _changed = UpdateClusterMembership(numberOfClusters, Yangben_arg, clusters);
            }

            Dictionary<string, Node> K_Means_Zi_Dian = new Dictionary<string, Node>();
            K_Means_Zi_Dian.Add("陕西", Yangben_arg[0]);
            K_Means_Zi_Dian.Add("甘肃", Yangben_arg[1]);
            K_Means_Zi_Dian.Add("宁夏", Yangben_arg[2]);
            K_Means_Zi_Dian.Add("青海", Yangben_arg[3]);
            K_Means_Zi_Dian.Add("新疆", Yangben_arg[4]);
            K_Means_Zi_Dian.Add("广西", Yangben_arg[5]);
            K_Means_Zi_Dian.Add("黑龙江", Yangben_arg[6]);
            K_Means_Zi_Dian.Add("吉林", Yangben_arg[7]);
            K_Means_Zi_Dian.Add("辽宁", Yangben_arg[8]);
            K_Means_Zi_Dian.Add("内蒙", Yangben_arg[9]);
            K_Means_Zi_Dian.Add("云南", Yangben_arg[10]);
            K_Means_Zi_Dian.Add("重庆", Yangben_arg[11]);
            Xiao_Zhi_Shu_List_four_year_dic.Add(K_Means_Zi_Dian);
            Yangben_arg_four_year.Add(Yangben_arg);
        }

        private bool UpdateClusterMembership(int _numberOfClusters, List<Node> _normalizedDataToCluster, List<Node> _clusters)
        {
            bool changed = false;

            double[] distances = new double[_numberOfClusters];

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < _normalizedDataToCluster.Count; ++i)
            {

                for (int k = 0; k < _numberOfClusters; ++k)
                    distances[k] = ElucidanDistance(_normalizedDataToCluster[i], _clusters[k]);

                int newClusterId = MinIndex(distances);
                if (newClusterId != _normalizedDataToCluster[i].Cluster)
                {
                    changed = true;
                    _normalizedDataToCluster[i].Cluster = newClusterId;
                }
            }
            if (changed == false)
                return false;
            if (EmptyCluster(_normalizedDataToCluster)) return false;
            return true;
        }

        private int MinIndex(double[] distances)
        {
            int _indexOfMin = 0;
            double _smallDist = distances[0];
            for (int k = 0; k < distances.Length; ++k)
            {
                if (distances[k] < _smallDist)
                {
                    _smallDist = distances[k];
                    _indexOfMin = k;
                }
            }
            return _indexOfMin;
        }

        private double ElucidanDistance(Node node, Node mean)
        {
            double _diffs = 0.0;
            _diffs = Math.Pow(node.Ji_Chu_Huan_Jing - mean.Ji_Chu_Huan_Jing, 2);
            _diffs += Math.Pow(node.Tou_Ru - mean.Tou_Ru, 2);
            _diffs += Math.Pow(node.Chan_chu - mean.Chan_chu, 2);
            _diffs += Math.Pow(node.Ji_xiao - mean.Ji_xiao, 2);
            return Math.Sqrt(_diffs);
        }

        private bool UpdateNodeMeans(List<Node> Yangben_arg, List<Node> clusters)
        {
            if (EmptyCluster(Yangben_arg)) return false;

            var groupToComputeMeans = Yangben_arg.GroupBy(s => s.Cluster).OrderBy(s => s.Key);
            int clusterIndex = 0;
            double jichuhuanjing = 0.0;
            double touru = 0.0;
            double chanchu = 0.0;
            double jixiao = 0.0;
            foreach (var item in groupToComputeMeans)
            {
                foreach (var value in item)
                {
                    jichuhuanjing += value.Ji_Chu_Huan_Jing;
                    touru += value.Tou_Ru;
                    chanchu += value.Chan_chu;
                    jixiao += value.Ji_xiao;
                }
                clusters[clusterIndex].Ji_Chu_Huan_Jing = jichuhuanjing / item.Count();
                clusters[clusterIndex].Tou_Ru = touru / item.Count();
                clusters[clusterIndex].Chan_chu = chanchu / item.Count();
                clusters[clusterIndex].Ji_xiao = jixiao / item.Count();
                clusterIndex++;
                jichuhuanjing = 0.0;
                touru = 0.0;
                chanchu = 0.0;
                jixiao = 0.0;
            }
            return true;
        }

        private bool EmptyCluster(List<Node> data)
        {
            var emptyCluster =
                data.GroupBy(s => s.Cluster).OrderBy(s => s.Key).Select(g => new { Cluster = g.Key, Count = g.Count() });

            foreach (var item in emptyCluster)
            {
                if (item.Count == 0)
                {
                    return true;
                }
            }
            return false;
        }

        private void InitializeCentroids(List<Node> Yangben_arg, int numberOfClusters)
        {
            Random random = new Random(numberOfClusters);
            for (int i = 0; i < numberOfClusters; ++i)
            {
                Yangben_arg[i].Cluster = i;
            }
            for (int i = numberOfClusters; i < Yangben_arg.Count; i++)
            {
                Yangben_arg[i].Cluster = random.Next(0, numberOfClusters);
            }
        }

        private void JI_XIAO(List<List<double>> listGroup, List<double> outputall, List<double> S_ij, int year, out Dictionary<string, double> Zhi_Shu_Zi_Dian, out List<double> Ji_Xiao_List)
        {
            listGroup.Clear();
            List<double> index4 = new List<double>(GetSubList(outputall, 96, 119));
            List<double> quan_zhong_xi_shu_list_4 = new List<double>();
            double temp_index_4 = S_ij[8] + S_ij[9];
            quan_zhong_xi_shu_list_4.Add(S_ij[8] / temp_index_4);
            quan_zhong_xi_shu_list_4.Add(S_ij[9] / temp_index_4);

            //Zhi_Shu_List.Clear();
            Ji_Xiao_List = new List<double>();
            listGroup = list_fen_zu1(index4, OBJECTNUM1);
            foreach (var item in listGroup)
            {
                Ji_Xiao_List.Add(Zhi_Shu(quan_zhong_xi_shu_list_4, item));
            }
            Console.WriteLine("\n");
            Console.WriteLine("{0:D}年小指数:绩效:", year);
            init_dictionary(out Zhi_Shu_Zi_Dian, Ji_Xiao_List);
            //PrintData(Zhi_Shu_Zi_Dian, Ji_Xiao_List);
        }

        private void CHAN_CHU(List<List<double>> listGroup, List<double> outputall, List<double> S_ij, int year, out Dictionary<string, double> Zhi_Shu_Zi_Dian, out List<double> Chan_Chu_List)
        {
            listGroup.Clear();
            List<double> index3 = new List<double>(GetSubList(outputall, 60, 95));
            List<double> quan_zhong_xi_shu_list_3 = new List<double>();
            double temp_index_3 = S_ij[5] + S_ij[6] + S_ij[7];
            quan_zhong_xi_shu_list_3.Add(S_ij[5] / temp_index_3);
            quan_zhong_xi_shu_list_3.Add(S_ij[6] / temp_index_3);
            quan_zhong_xi_shu_list_3.Add(S_ij[7] / temp_index_3);

            //Zhi_Shu_List.Clear();
            Chan_Chu_List = new List<double>();
            listGroup = list_fen_zu1(index3, OBJECTNUM1);
            foreach (var item in listGroup)
            {
                Chan_Chu_List.Add(Zhi_Shu(quan_zhong_xi_shu_list_3, item));
            }
            Console.WriteLine("\n");
            Console.WriteLine("{0:D}年小指数:产出:", year);
            init_dictionary(out Zhi_Shu_Zi_Dian, Chan_Chu_List);
            //PrintData(Zhi_Shu_Zi_Dian, Chan_Chu_List);
        }

        private void TOU_RU(List<List<double>> listGroup, List<double> outputall, List<double> S_ij, int year, out Dictionary<string, double> Zhi_Shu_Zi_Dian, out List<double> Tou_Ru__List)
        {
            listGroup.Clear();
            List<double> index2 = new List<double>(GetSubList(outputall, 24, 59));
            List<double> quan_zhong_xi_shu_list_2 = new List<double>();
            double temp_index_2 = S_ij[2] + S_ij[3] + S_ij[4];
            quan_zhong_xi_shu_list_2.Add(S_ij[2] / temp_index_2);
            quan_zhong_xi_shu_list_2.Add(S_ij[3] / temp_index_2);
            quan_zhong_xi_shu_list_2.Add(S_ij[4] / temp_index_2);
            //Quan_Zhong_xi_shu(shang_list, out quan_zhong_xi_shu_list);

            //Zhi_Shu_List.Clear();
            Tou_Ru__List = new List<double>();
            listGroup = list_fen_zu1(index2, OBJECTNUM1);
            foreach (var item in listGroup)
            {
                Tou_Ru__List.Add(Zhi_Shu(quan_zhong_xi_shu_list_2, item));
            }
            //Console.WriteLine("\n");
            //Console.WriteLine("{0:D}年小指数:投入:", year);
            init_dictionary(out Zhi_Shu_Zi_Dian, Tou_Ru__List);
            //PrintData(Zhi_Shu_Zi_Dian, Tou_Ru__List);
        }

        private void JI_CHU_HUAN_JING(List<List<double>> listGroup, List<double> outputall, List<double> S_ij, int year, out Dictionary<string, double> Zhi_Shu_Zi_Dian, out List<double> Ji_Chu_Huan_Jing_List)
        {

            listGroup.Clear();
            List<double> index1 = new List<double>(GetSubList(outputall, 0, 23));
            List<double> quan_zhong_xi_shu_list_1 = new List<double>();
            double temp_index_1 = S_ij[0] + S_ij[1];
            quan_zhong_xi_shu_list_1.Add(S_ij[0] / temp_index_1);
            quan_zhong_xi_shu_list_1.Add(S_ij[1] / temp_index_1);

            //Zhi_Shu_List.Clear();
            Ji_Chu_Huan_Jing_List = new List<double>();
            listGroup = list_fen_zu1(index1, OBJECTNUM1);
            foreach (var item in listGroup)
            {
                Ji_Chu_Huan_Jing_List.Add(Zhi_Shu(quan_zhong_xi_shu_list_1, item));
            }
            //Console.WriteLine("\n");
            //Console.WriteLine("{0:D}年小指数:基础环境:", year);
            init_dictionary(out Zhi_Shu_Zi_Dian, Ji_Chu_Huan_Jing_List);
            //PrintData(Zhi_Shu_Zi_Dian, Ji_Chu_Huan_Jing_List);
        }

        private List<double> GetSubList(List<double> holelist, int fromIndex, int toIndex)
        {
            List<double> result = new List<double>();
            for (int i = fromIndex; i <= toIndex; i++)
            {
                result.Add(holelist[i]);
            }
            return result;
        }

        private void Quan_Zhong_xi_shu(List<double> input_index, out List<double> output_index)
        {
            output_index = new List<double>();

            double sum = 0;
            foreach (var item in input_index)
            {
                sum += (1 - item);
            }

            foreach (var item in input_index)
            {
                output_index.Add((1 - item) / sum);
            }
        }

        private void Compute_Shang_List(List<List<double>> listGroup, out List<double> shang_list)
        {
            double temp = 0;
            shang_list = new List<double>();
            for (int i = 0; i < IndexNUM; i++)
            {
                temp = Te_Zheng_Bi_Zhong_and_Shang(listGroup[i]);
                shang_list.Add(temp);
            }
        }

        private double Te_Zheng_Bi_Zhong_and_Shang(List<double> input_index)
        {
            //特征比重
            //output_index = new List<double>();
            List<double> temp = new List<double>();
            double sum1 = 0;
            double sum2 = 0;
            double shang = 0;
            //double w = 0;
            foreach (var item in input_index)
            {
                sum1 += item;
            }
            foreach (var item in input_index)
            {
                temp.Add(item / sum1);
            }
            //熵值
            foreach (var item in temp)
            {
                if (item == 0)
                {
                    continue;
                }
                sum2 += item * System.Math.Log(item);
            }
            shang = sum2 * (-1 / System.Math.Log(OBJECTNUM));
            return shang;
        }

        private void init_input_and_output_data(double[][] input, double[][] output, List<List<double>> listGroup, List<double> Zhi_Shu_List, int num)
        {
            int i, k = 0;
            for (i = num * OBJECTNUM1, k = 0; i < (num + 1) * OBJECTNUM1; i++, k++)
            {
                input[i] = new double[IndexNUM];
                output[i] = new double[1];
                for (int j = 0; j < IndexNUM; j++)
                {
                    input[i][j] = listGroup[k][j];
                }
                output[i][0] = Zhi_Shu_List[k];
            }
        }

        private void init_dictionary(out Dictionary<string, double> Zhi_Shu_Zi_Dian, List<double> Zhi_Shu_List)
        {
            Zhi_Shu_Zi_Dian = new Dictionary<string, double>();
            Zhi_Shu_Zi_Dian.Add("陕西", Zhi_Shu_List[0]);
            Zhi_Shu_Zi_Dian.Add("甘肃", Zhi_Shu_List[1]);
            Zhi_Shu_Zi_Dian.Add("宁夏", Zhi_Shu_List[2]);
            Zhi_Shu_Zi_Dian.Add("青海", Zhi_Shu_List[3]);
            Zhi_Shu_Zi_Dian.Add("新疆", Zhi_Shu_List[4]);
            Zhi_Shu_Zi_Dian.Add("广西", Zhi_Shu_List[5]);
            Zhi_Shu_Zi_Dian.Add("黑龙江", Zhi_Shu_List[6]);
            Zhi_Shu_Zi_Dian.Add("吉林", Zhi_Shu_List[7]);
            Zhi_Shu_Zi_Dian.Add("辽宁", Zhi_Shu_List[8]);
            Zhi_Shu_Zi_Dian.Add("内蒙", Zhi_Shu_List[9]);
            Zhi_Shu_Zi_Dian.Add("云南", Zhi_Shu_List[10]);
            Zhi_Shu_Zi_Dian.Add("重庆", Zhi_Shu_List[11]);
        }

        private List<double> Compute_ZhiShu_List(List<List<double>> listGroup, List<double> quan_zhong_xi_shu_list)
        {
            List<double> Zhi_Shu_List = new List<double>();
            foreach (var item in listGroup)
            {
                Zhi_Shu_List.Add(Zhi_Shu(item, quan_zhong_xi_shu_list));
            }
            return Zhi_Shu_List;
        }

        private double Zhi_Shu(List<double> input_index, List<double> input_quanzhong)
        {
            double sum = 0;
            for (int i = 0; i < input_index.Count(); i++)
            {
                sum += input_index[i] * input_quanzhong[i];
            }
            return sum;
        }

        private void Compute_S_ij(out List<double> S_ij, List<double> R_ij)
        {
            S_ij = new List<double>();
            double sum = 0;
            for (int i = 0; i < Input_layer; i++)
            {
                sum += R_ij[i];
            }
            for (int j = 0; j < Input_layer; j++)
            {
                S_ij.Add(R_ij[j] / sum);
            }
        }

        private void Compute_R_ij(out List<double> R_ij, List<double> r_ij)
        {
            R_ij = new List<double>();
            for (int i = 0; i < Input_layer; i++)
            {
                double item = 0;
                item = Math.Abs((1 - Math.Exp(-r_ij[i])) / (1 + Math.Exp(-r_ij[i])));
                R_ij.Add(item);
            }
        }

        private void Compute_r_ij(out List<double> r_ij, double[][] W_ki, double[][] W_jk)
        {
            double[][] newW_ki = Rotate(W_ki);
            r_ij = new List<double>();
            for (int i = 0; i < Input_layer; i++)
            {
                double item = 0;
                for (int k = 0; k < Hidden_layer; k++)
                {
                    item += newW_ki[i][k] * (1 - Math.Exp(-W_jk[0][k])) / (1 + Math.Exp(-W_jk[0][k]));
                }
                r_ij.Add(item);
            }
        }

        private double[][] Rotate(double[][] array)
        {
            double[][] newArray = new double[array[0].Length][]; //构造转置二维数组
            for (int i = 0; i < array[0].Length; i++)
            {
                newArray[i] = new double[array.Length];
            }
            for (int i = 0; i < newArray.Length; i++)
            {
                for (int j = 0; j < newArray[0].Length; j++)
                {
                    newArray[i][j] = array[j][i];
                }
            }
            return newArray;
        }

        private void SearchSolution(out BackPropagationLearning teacher, double[][] input, double[][] output)
        {
            int gen = 0;
            double error = 0;
            ActivationNetwork network = new ActivationNetwork(
                new BipolarSigmoidFunction(2),
                Input_layer, Hidden_layer, Output_layer);
            teacher = new BackPropagationLearning(network);
            int iteration = 1;

            error = 1.0;
            while (error > 0.001)
            {
                error = teacher.RunEpoch(input, output) / OBJECTNUM;
                iteration++;
                if ((iterations != 0) && (iteration > iterations))
                    break;
            }
        }

        private List<List<double>> list_fen_zu1(List<double> input, int zuNum)
        {
            List<List<double>> listGroup = new List<List<double>>();
            int j = 0;
            for (int i = 0; i < zuNum; i++)
            {
                List<double> cList = new List<double>();
                j = i;
                while (j < input.Count())
                {
                    cList.Add(input[j]);
                    j += zuNum;
                }
                listGroup.Add(cList);
            }
            return listGroup;
        }

        private List<double> Normalization(List<List<double>> listGroup)
        {
            //  计算出正向指标，结果输出到 output
            List<double> outputall = new List<double>();
            List<double> output;
            for (int i = 0; i < listGroup.Count() - 1; i++)
            {
                Cal_Forward_Indicator(listGroup[i], out output);
                foreach (var item in output)
                {
                    //Console.WriteLine(item);
                    outputall.Add(item);
                }
            }

            //  计算出逆向指标，结果输出到 outputlist
            List<double> outputlist;
            Cal_Back_Indicator(listGroup[listGroup.Count() - 1], out outputlist);
            foreach (var item in outputlist)
            {
                //Console.WriteLine(item);
                outputall.Add(item);
            }
            return outputall;
        }

        private void Cal_Forward_Indicator(List<double> input_index, out List<double> output_index)
        {
            output_index = new List<double>();

            foreach (var item in input_index)
            {
                output_index.Add((item - input_index.Min()) / (input_index.Max() - input_index.Min()));
            }
        }

        private void Cal_Back_Indicator(List<double> input_index, out List<double> output_index)
        {
            output_index = new List<double>();

            foreach (var item in input_index)
            {
                output_index.Add((input_index.Max() - item) / (input_index.Max() - input_index.Min()));
            }
        }

        private List<List<double>> list_fen_zu(List<double> input, int zuNum, int objctnum)
        {
            List<List<double>> listGroup = new List<List<double>>();
            for (int i = 0; i < zuNum; i++)
            {
                List<double> cList = new List<double>();
                for (int j = 0 + i * objctnum; j < objctnum + i * objctnum; j++)
                {

                    cList.Add(input[j]);
                }
                listGroup.Add(cList);
            }
            return listGroup;
        }

        private List<double> Split_All_Year_Data_To_Eachyear(List<double> AllData, int k)
        {
            List<double> ReturnList = new List<double>();
            while (ReturnList.Count() < OBJECTNUM1 * IndexNUM)
            {
                ReturnList.Add(AllData[k]);
                k += 4;
            }
            return ReturnList;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                textBox1.Clear();
                textBox2.Clear();
                string year = comboBox1.Items[0].ToString();
                string index = comboBox2.Items[0].ToString();
                List<double> temp = new List<double>();
                int i = 0;

                if (comboBox1.SelectedItem != null)
                {
                    year = comboBox1.SelectedItem.ToString();
                }
                if (comboBox2.SelectedItem != null)
                {
                    index = comboBox2.SelectedItem.ToString();
                }
                switch (year)
                {
                    case "2011":
                        i = 0;
                        break;
                    case "2012":
                        i = 1;
                        break;
                    case "2013":
                        i = 2;
                        break;
                    case "2014":
                        i = 3;
                        break;
                    default:
                        break;
                }
                foreach (KeyValuePair<string, double> kvp in Zhi_Shu_List_four_year_dic[i])
                {
                    textBox1.AppendText("省份：" + kvp.Key + ",指数：" + kvp.Value + "\r\n");
                }
                textBox2.AppendText("------------------------------\n");
                textBox2.AppendText("K-Means算法分组如下：\n");
                var group = Yangben_arg_four_year[i].GroupBy(s => s.Cluster).OrderBy(s => s.Key);
                foreach (var g in group)
                {
                    textBox2.AppendText("Cluster # " + g.Key + ":\n");
                    foreach (var value in g)
                    {
                        foreach (KeyValuePair<string, Node> kv in Xiao_Zhi_Shu_List_four_year_dic[i])
                        {

                            if (value.Equals(kv.Value))
                            {
                                textBox2.AppendText(kv.Key);
                                textBox2.AppendText("\n");
                            }
                        }
                    }
                    textBox2.AppendText("------------------------------\n");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("请先读取数据","操作异常", MessageBoxButtons.OK,
                MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
            }
            
        }

        private void button3_Click(object sender, EventArgs e)
        {
            inputCount = Convert.ToInt32(txtInputCount.Text);
            neuronCount[0] = inputCount;
            neuronCount[1] = Convert.ToInt32(txtHiddenCount.Text);
            neuronCount[2] = 1;
            
        }

        private void btnTrain_Click(object sender, EventArgs e)
        {
            try
            {
                txtRuninfo.Clear();
                DE de = new DE();
                int gen = 0;
                int maxCycle = 2000;
                double error = 0;
                double minError = 0.001;
                double minDeError = 0.1;
                double[][] input_copy = new double[OBJECTNUM][];
                double[][] output_copy = new double[OBJECTNUM][];
                input_copy = input;
                output_copy = output;
                trainData = new double[trainCount, inputCount];
                targetData = new double[trainCount];
                string select = comboBox3.Items[1].ToString();
                myNetwork = new ActivationNetwork(new BipolarSigmoidFunction(2), inputCount, neuronCount[1], neuronCount[2]);

                if (comboBox3.SelectedItem != null)
                {
                    select = comboBox3.SelectedItem.ToString();
                }

                for (int i = 0; i < trainCount; ++i)
                {
                    for (int j = 0; j < inputCount; ++j)
                    {
                        trainData[i, j] = input[i][j];
                    }
                    targetData[i] = output[i][0];
                }
                switch (select)
                {
                    case "Yes":

                        break;
                    case "No":
                        de.Initialize(myNetwork, trainData, targetData);
                        while (gen <= maxCycle)
                        {

                            de.Mutation();
                            de.CrossOver();
                            de.Selection();
                            gen++;
                            error = de.SaveBest();
                            if (error < minDeError)
                                break;

                        }
                        break;
                    default:
                        break;
                }

                //使用BP训练
                BackPropagationLearning teacher = new BackPropagationLearning(myNetwork);
                error = 1.0;
                gen = 0;
                while (error > minError)
                {
                    error = teacher.RunEpoch(input_copy, output_copy) / OBJECTNUM;

                    gen++;
                    if ((iterations != 0) && (gen > iterations))
                        break;
                }
                txtRuninfo.AppendText("BP误差：" + error + ",训练次数为：" + gen + "\r\n");
                txtRuninfo.AppendText("训练完毕！\r\n");
            }
            catch (Exception ex)
            {
                MessageBox.Show("请先读取数据", "操作异常", MessageBoxButtons.OK,
                MessageBoxIcon.Warning, MessageBoxDefaultButton.Button1);
            }
        }
    }
}
