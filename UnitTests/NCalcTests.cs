using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using NCalc;

using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class NCalcTests
    {
        [Test]
        public void Test1()
        {
            var expression = new Expression("Round(Pow([Pi], 2) + Pow([Pi2], 2) + [X], 2)");

            expression.Parameters["Pi2"] = new Expression("Pi * [Pi]");
            expression.Parameters["X"] = 10;

            expression.DynamicParameters["Pi"] = _ =>
            {
                Console.WriteLine("I'm evaluating π!");
                return 3.14;
            };

            Assert.That(expression.Evaluate(), Is.EqualTo(117.07d).Within(0.001d));


        }

        [Test]
        public void Ax() // note that this version requires parameter y
        {
            var expression = new Expression("( ([z1] + [z2] ) / (2 * Cos ([beta])) +[y] )  * [mn] ");
            var z1 = 12;
            var z2 = 60;
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            ;
            var xn1 = 0.09809;
            var xn2 = 0.0;
            var y = 0.09744;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["mn"] = mn;
            expression.Parameters["y"] = y;
            Assert.That(expression.Evaluate(), Is.EqualTo(125.0d).Within(0.0001d));

        }

        [Test]
        public void Y() // note that this version requires parameter ax for centre distance
        {
            var expression = new Expression(" (([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta])))) ");

            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            ;

            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["mn"] = mn;

            Assert.That(expression.Evaluate(), Is.EqualTo(0.097447).Within(0.0001d));

        }

        [Test]
        public void Y2() // alternative using pressure angles
        {

            // var s1 = "([z1]+[z2])";

            // var s2 = "(2*Cos([beta]))";
            // var s3 = "(Cos(Atan(Tan([alphan])/Cos([beta]))))";
            // var s4 = "(Cos(Acos( ((z1+z2) * Cos( Atan(Tan([alphan])/Cos([beta])))) / ((z1+z2) + (2 * (([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta]))))) * Cos([beta]) )))";
            // var s5 = "-1";

            var s =
                "(([z1]+[z2])/(2*Cos([beta]))) * ((Cos(Atan(Tan([alphan])/Cos([beta]))))/(Cos(Acos( ((z1+z2) * Cos( Atan(Tan([alphan])/Cos([beta])))) / ((z1+z2) + (2 * (([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta]))))) * Cos([beta]) ))))-1) ";
            // sz is simplified form
            var sz =
                "(([z1] + [z2]) * ((z1 + z2) + (2 * (([ax] / [mn]) - (([z1] + [z2]) / (2 * Cos([beta]))))) * Cos([beta]))) / ((2 * Cos([beta])) * (z1 + z2)) - (([z1] + [z2]) / (2 * Cos([beta])))";

            var expression = new Expression(sz);

            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            ;
            var alphan = 20 * (Math.PI / 180.0);
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;

            Assert.That(expression.Evaluate(), Is.EqualTo(0.097447).Within(0.0001d));

        }

        [Test]
        public void Alphat()
        {
            var expression = new Expression(" Atan(Tan([alphan])/Cos([beta])) ");
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["beta"] = beta;
            var result = (double)expression.Evaluate() * (180.0 / Math.PI);

            Assert.That(result, Is.EqualTo(22.79587d).Within(0.0001d));

        }

        [Test]
        public void Alphawt()
        {
            var expression =
                new Expression(
                    "Acos((([z1]+[z2])*Cos(Atan(Tan([alphan])/Cos([beta]))))/(([z1]+[z2])+(2*(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta])))))*Cos([beta])))");
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            ;
            var xn1 = 0.09809;
            var xn2 = 0.0;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            // expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate() * (180.0 / Math.PI);
            Assert.That(result, Is.EqualTo(23.11263d).Within(0.0001d));

        }

        [Test]
        public void InvAlphawt()
        {
            // var invAlpahwt =
            //     " Tan(Acos((([z1]+[z2])*Cos(Atan(Tan([alphan])/Cos([beta]))))/(([z1]+[z2])+(2*(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta])))))*Cos([beta])))) - Acos((([z1]+[z2])*Cos(Atan(Tan([alphan])/Cos([beta]))))/(([z1]+[z2])+(2*(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta])))))*Cos([beta])))";
            // good

            var invAlphawt =
                ("(2*Tan([alphan])*(([xn1]+[xn2])/([z1]+[z2])))+(Tan(Atan(Tan([alphan])/Cos([beta])))-Atan(Tan([alphan])/Cos([beta])))"); //good

            var expression = new Expression(invAlphawt);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            ;
            var xn1 = 0.09809;
            var xn2 = 0.0;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            // expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(0.023405d).Within(0.0001d));

        }

        [Test]
        public void InvAlphat() //good
        {

            var invAlpaht = "Tan(Atan(Tan([alphan])/Cos([beta]))) - Atan(Tan([alphan])/Cos([beta])) "; //good

            var expression = new Expression(invAlpaht);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            ;
            var xn1 = 0.09809;
            var xn2 = 0.0;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            // expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(0.02241351d).Within(0.0001d));

        }

        [Test]
        public void Sigmax()
        {

            var sumZ1Z2 = "([z1]+[z2])";
            var invAlpaht = "(Tan(Atan(Tan([alphan])/Cos([beta]))) - Atan(Tan([alphan])/Cos([beta]))) ";
            var invAlpahwt =
                "(2*Tan([alphan])*(([xn1]+[xn2])/([z1]+[z2])))+(Tan(Atan(Tan([alphan])/Cos([beta])))-Atan(Tan([alphan])/Cos([beta])))";
            var SigmaX =
                "((([z1]+[z2])*((2*Tan([alphan])*(([xn1]+[xn2])/([z1]+[z2])))+(Tan(Atan(Tan([alphan])/Cos([beta])))-Atan(Tan([alphan])/Cos([beta])))-(Tan(Atan(Tan([alphan])/Cos([beta])))-Atan(Tan([alphan])/Cos([beta])))))/(2*Tan([alphan])))"; //good

            var expression = new Expression(SigmaX);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            ;
            var xn1 = 0.09809;
            var xn2 = 0.0;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            // expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(0.098089d).Within(0.0001d));

        }


        [Test]
        public void H()
        {
            //  \left[2.25+y-\left(x_{n1}+x_{n2}\right)\right]m_{n}    

            var h = "(2.25+(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta]))))-([xn1]+[xn2])) *[mn]";

            var expression = new Expression(h);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            ;
            var xn1 = 0.09809;
            var xn2 = 0.0;
            // var y = 0.09744;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            //  expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(6.7480d).Within(0.0001d));

        }

        [Test]
        public void d1()
        {


            var d1 = "([z1]*[mn])/Cos([beta])";

            var expression = new Expression(d1);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            ;
            var xn1 = 0.09809;
            var xn2 = 0.0;
            // var y = 0.09744;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            //  expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(41.5692d).Within(0.0001d));

        }

        [Test]
        public void d2()
        {


            var d2 = "([z2] * [mn]) / Cos([beta])  ";

            var expression = new Expression(d2);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            ;
            var xn1 = 0.09809;
            var xn2 = 0.0;
            // var y = 0.09744;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            //  expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(207.8460d).Within(0.0001d));

        }

        [Test]
        public void db2()
        {

            var db2 = "(([z2]*[mn])/Cos([beta]))*Cos(Atan(Tan([alphan])/Cos([beta])))";

            var expression = new Expression(db2);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            ;
            var xn1 = 0.09809;
            var xn2 = 0.0;
            // var y = 0.09744;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            //  expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(191.61145d).Within(0.0001d));

        }

        [Test]
        public void db1()
        {

            var db1 = "(([z1]*[mn])/Cos([beta]))*Cos(Atan(Tan([alphan])/Cos([beta]))) ";

            var expression = new Expression(db1);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            ;
            var xn1 = 0.09809;
            var xn2 = 0.0;
            // var y = 0.09744;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            //  expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(38.3222d).Within(0.0001d));

        }

        [Test]
        public void dw1()
        {
            var dw1 =
                "(([z1]*[mn])/Cos([beta]))*Cos(Atan(Tan([alphan])/Cos([beta]))) / Cos(Acos((([z1]+[z2])*Cos(Atan(Tan([alphan])/Cos([beta]))))/(([z1]+[z2])+(2*(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta])))))*Cos([beta]))))";

            var expression = new Expression(dw1);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            ;
            var xn1 = 0.09809;
            var xn2 = 0.0;
            // var y = 0.09744;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            //  expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(41.6666d).Within(0.0001d));

        }

        [Test]
        public void dw2()
        {
            var dw2 =
                "(([z2]*[mn])/Cos([beta]))*Cos(Atan(Tan([alphan])/Cos([beta]))) / Cos(Acos((([z1]+[z2])*Cos(Atan(Tan([alphan])/Cos([beta]))))/(([z1]+[z2])+(2*(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta])))))*Cos([beta]))))";

            var expression = new Expression(dw2);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            ;
            var xn1 = 0.09809;
            var xn2 = 0.0;
            // var y = 0.09744;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            //  expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(208.3333).Within(0.0001d));

        }

        [Test]
        public void ha1()
        {
            var ha1 = "(1+(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta]))))-[xn2])*[mn]";

            var expression = new Expression(ha1);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            ;
            var xn1 = 0.09809;
            var xn2 = 0.0;
            // var y = 0.09744;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            //  expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(3.2923).Within(0.0001d));

        }

        [Test]
        public void ha2()
        {
            var ha2 = "(1 + (([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta])))) - [xn1]) * [mn]";

            var expression = new Expression(ha2);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
            ;
            var xn1 = 0.09809;
            var xn2 = 0.0;
            // var y = 0.09744;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            //  expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(2.9980).Within(0.0001d));

        }

        [Test]
        public void hf1()
        {
            var hf1 =
                "((2.25+(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta]))))-([xn1]+[xn2])) *[mn])-((1+(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta]))))-[xn2])*[mn])";
            var expression = new Expression(hf1);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
           
            var xn1 = 0.09809;
            var xn2 = 0.0;
            // var y = 0.09744;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            //  expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(3.4557).Within(0.0001d));
        }
        
        [Test]
        public void hf2()
        {
            var hf1 =
                "((2.25+(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta]))))-([xn1]+[xn2])) *[mn])-((1+(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta]))))-[xn1])*[mn])";
            var expression = new Expression(hf1);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
           
            var xn1 = 0.09809;
            var xn2 = 0.0;
            // var y = 0.09744;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            //  expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(3.75).Within(0.0001d));
        }
        
        [Test]
        public void da1()
        {
            var da1 =
                "([z1]*[mn])/Cos([beta])  + (2 * ((1+(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta]))))-[xn2])*[mn]))";
            var expression = new Expression(da1);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
           
            var xn1 = 0.09809;
            var xn2 = 0.0;
            // var y = 0.09744;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            //  expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(48.1539).Within(0.0001d));
        }
        
        [Test]
        public void da2()
        {
            var da2 =
                "([z2]*[mn])/Cos([beta])  + (2 * ((1+(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta]))))-[xn1])*[mn]))";
            var expression = new Expression(da2);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
           
            var xn1 = 0.09809;
            var xn2 = 0.0;
            // var y = 0.09744;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            //  expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(213.8422).Within(0.0001d));
        }
        
        [Test]
        public void df1()
        {
            var df1 =
                "([z1]*[mn])/Cos([beta])  + (2 * ((1+(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta]))))-[xn2])*[mn])) - ( 2* (2.25+(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta]))))-([xn1]+[xn2])) *[mn])";
            var expression = new Expression(df1);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
           
            var xn1 = 0.09809;
            var xn2 = 0.0;
            // var y = 0.09744;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            //  expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(34.6577).Within(0.0001d));
        }
        
        [Test]
        public void df2()
        {
            var df1 =
                "([z2]*[mn])/Cos([beta])  + (2 * ((1+(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta]))))-[xn1])*[mn])) - ( 2* (2.25+(([ax]/[mn])-(([z1]+[z2])/(2*Cos([beta]))))-([xn1]+[xn2])) *[mn])";
            var expression = new Expression(df1);
            var ax = 125d;
            var z1 = 12;
            var z2 = 60;
            var alphan = 20 * (Math.PI / 180.0);
            var beta = 30 * (Math.PI / 180.0);
            var mn = 3;
           
            var xn1 = 0.09809;
            var xn2 = 0.0;
            // var y = 0.09744;
            expression.Parameters["xn1"] = xn1;
            expression.Parameters["xn2"] = xn2;
            expression.Parameters["ax"] = ax;
            expression.Parameters["z1"] = z1;
            expression.Parameters["z2"] = z2;
            expression.Parameters["beta"] = beta;
            expression.Parameters["alphan"] = alphan;
            expression.Parameters["mn"] = mn;
            //  expression.Parameters["y"] = y;
            var result = (double)expression.Evaluate();
            Assert.That(result, Is.EqualTo(200.3460).Within(0.0001d));
        }
    }
}