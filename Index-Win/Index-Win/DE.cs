using AForge.Neuro;
using System;

namespace Index_Win
{
    public class DE
    {
        public static int NP = 80;// 种群规模  
        public static int size = 10;// 个体的长度  
        public static int xMin = 0;// 最小值  
        public static int xMax = 1;// 最大值  
        public static double F = 0.5;// 变异的控制参数  
        public static double CR = 0.9;// 杂交的控制参数  
        private double[,] trainData;    //训练数据
        private double[] targetData;    //期望数据
        private double[,] veryData;     //验证输入数据
        private double[] outData;       //验证输出数据
        private double[,] X;// 个体  
        private double[,] XMutation;
        private double[,] XCrossOver;
        private double[] fitness_X = new double[NP];// 适应值  
        private double[] global_bestX;
        private double gloobal_best_value;
        private ActivationNetwork network;
        public double[,] getX()
        {
            return X;
        }

        /** 
         * 矩阵的复制 
         *  
         * @param x把x复制给个体 
         */
        public void setX(double[,] x)
        {
            for (int i = 0; i < NP; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    this.X[i, j] = x[i, j];
                }
            }
        }

        public double[] getFitness_X()
        {
            return fitness_X;
        }

        public void setFitness_X(double[] fitness_X)
        {
            for (int i = 0; i < NP; i++)
            {
                this.fitness_X[i] = fitness_X[i];
            }
        }

        public double[,] getXMutation()
        {
            return XMutation;
        }

        public void setXMutation(double[,] xMutation)
        {
            for (int i = 0; i < NP; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    this.XMutation[i, j] = xMutation[i, j];
                }
            }
        }

        public double[,] getXCrossOver()
        {
            return XCrossOver;
        }

        public void setXCrossOver(double[,] xCrossOver)
        {
            for (int i = 0; i < NP; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    this.XCrossOver[i, j] = xCrossOver[i, j];
                }
            }
        }

        /** 
         * 适应值的计算 
         *  
         * @param XTemp根据个体计算适应值 
         * @return返回适应值 
         */
        public double CalculateFitness(double[] XTemp)
        {
            double fitness = 0;
            int count = 0;
            //把值放到神经网络上面
            for (int i = 1; i < network.Layers.Length; ++i)
            {
                for (int j = 0; j < network.Layers[i].Neurons.Length; ++j)
                {
                    for (int k = 0; k < network.Layers[i].Neurons[j].Weights.Length; ++k)
                    {
                        network.Layers[i].Neurons[j].Weights[k] = XTemp[count];
                        count++;
                    }
                }
            }
            //求目前网络权值下面，所求得的误差
            int dataLen = trainData.GetLength(1);
            double[] temp = new double[dataLen];
            double result;
            for (int i = 0; i < trainData.GetLength(0); ++i)
            {
                for (int j = 0; j < dataLen; ++j)
                {
                    temp[j] = trainData[i, j];
                }
                // trainData.CopyTo(temp, i * dataLen);
                result = network.Compute(temp)[0];
                fitness += (result - targetData[i])
                    * (result - targetData[i]);

            }
            fitness = Math.Sqrt(fitness / trainData.GetLength(0));

            return fitness;
        }

        /** 
         * 初始化：随机初始化种群，计算个体的适应值 
         */
        public void Initialize(ActivationNetwork net, double[,] train, double[] target)
        {
            this.network = net;
            this.trainData = train;
            this.targetData = target;
            size = 0;
            for (int i = 1; i < net.Layers.Length; ++i)
            {
                size += net.Layers[i - 1].Neurons.Length * net.Layers[i].Neurons.Length;
            }
            global_bestX = new double[size];

            X = new double[NP, size];// 个体  
            XMutation = new double[NP, size];
            XCrossOver = new double[NP, size];
            fitness_X = new double[NP];// 适应值  
            double[,] XTemp = new double[NP, size];
            double[] FitnessTemp = new double[NP];
            double[] CalTempArray = new double[size];
            gloobal_best_value = 1e3;
            int bestIndex = 0;
            for (int i = 0; i < NP; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    XTemp[i, j] = xMin + r.NextDouble() * (xMax - xMin);
                }
                // 计算适应值  
                for (int j = 0; j < size; ++j)
                {
                    CalTempArray[j] = XTemp[i, j];
                }

                FitnessTemp[i] = CalculateFitness(CalTempArray);
                if (FitnessTemp[i] < gloobal_best_value)
                {
                    gloobal_best_value = FitnessTemp[i];
                    bestIndex = i;
                }
            }
            for (int i = 0; i < size; ++i)
            {
                global_bestX[i] = XTemp[bestIndex, i];
            }

            this.setX(XTemp);
            this.setFitness_X(FitnessTemp);
        }
        private Random r = new Random();





        /******** 变异操作 ***********/
        public void Mutation()
        {
            double[,] XTemp = new double[NP, size];
            double[,] XMutationTemp = new double[NP, size];
            XTemp = this.getX();

            for (int i = 0; i < NP; i++)
            {
                int r1 = 0, r2 = 0, r3 = 0;
                while (r1 == i || r2 == i || r3 == i || r1 == r2 || r1 == r3
                        || r2 == r3)
                {// 取r1,r2,r3  
                    r1 = r.Next(NP);
                    r2 = r.Next(NP);
                    r3 = r.Next(NP);

                }
                for (int j = 0; j < size; j++)
                {
                    XMutationTemp[i, j] = XTemp[r1, j] + F
                            * (XTemp[r2, j] - XTemp[r3, j]);
                }
            }
            this.setXMutation(XMutationTemp);
        }

        /** 
         * 交叉操作 
         */
        public void CrossOver()
        {
            double[,] XTemp = new double[NP, size];
            double[,] XMutationTemp = new double[NP, size];
            double[,] XCrossOverTemp = new double[NP, size];

            XTemp = this.getX();
            XMutationTemp = this.getXMutation();
            // 交叉操作  

            for (int i = 0; i < NP; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    double rTemp = r.NextDouble();
                    if (rTemp <= CR)
                    {
                        XCrossOverTemp[i, j] = XMutationTemp[i, j];
                    }
                    else
                    {
                        XCrossOverTemp[i, j] = XTemp[i, j];
                    }
                }
            }
            this.setXCrossOver(XCrossOverTemp);
        }

        /** 
         * 选择操作：使用贪婪选择策略 
         */
        public void Selection()
        {
            double[,] XTemp = new double[NP, size];
            double[,] XCrossOverTemp = new double[NP, size];
            double[] FitnessTemp = new double[NP];
            double[] FitnessCrossOverTemp = new double[NP];
            double[] CalTempArray = new double[size];
            XTemp = this.getX();
            XCrossOverTemp = this.getXCrossOver();// 交叉变异后的个体  
            FitnessTemp = this.getFitness_X();

            // 对群体进行重新设置  
            for (int i = 0; i < NP; i++)
            {
                for (int j = 0; j < size; ++j)
                {
                    CalTempArray[j] = XCrossOverTemp[i, j];
                }
                FitnessCrossOverTemp[i] = CalculateFitness(CalTempArray);
                if (FitnessCrossOverTemp[i] < FitnessTemp[i])
                {
                    for (int j = 0; j < size; j++)
                    {
                        XTemp[i, j] = XCrossOverTemp[i, j];
                    }
                    FitnessTemp[i] = FitnessCrossOverTemp[i];
                }
            }
            this.setX(XTemp);
            this.setFitness_X(FitnessTemp);
        }

        /** 
         * 保存每一代的全局最优值 
         */
        public double SaveBest()
        {
            double[] FitnessTemp = new double[NP];
            FitnessTemp = this.getFitness_X();
            int temp = 0;
            // 找出最小值  
            for (int i = 1; i < NP; i++)
            {
                if (FitnessTemp[temp] > FitnessTemp[i])
                {
                    temp = i;
                }
            }
            int count = 0;
            //把值放到神经网络上面
            for (int i = 1; i < network.Layers.Length; ++i)
            {
                for (int j = 0; j < network.Layers[i].Neurons.Length; ++j)
                {
                    for (int k = 0; k < network.Layers[i].Neurons[j].Weights.Length; ++k)
                    {
                        network.Layers[i].Neurons[j].Weights[k] = X[temp, count];
                        count++;
                    }
                }
            }
            return FitnessTemp[temp];
        }
    }
}