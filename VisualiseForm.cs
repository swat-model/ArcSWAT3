using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace ArcSWAT3
{
    public partial class VisualiseForm : Form
    {
        private Visualise _parent;

        public VisualiseForm(Visualise parent) {
            InitializeComponent();
            this._parent = parent;
        }

        public void removeAnimationMode() {
            this.animationMode.Visible = false;
            this.composeOptions.Visible = false;
        }

        public void setCompare(bool enabled) {
            this.compareGroup.Visible = enabled;
        }

        public ComboBox PscenariosCombo => this.scenariosCombo;

        public ComboBox PoutputCombo => this.outputCombo;

        public  ComboBox PvariablePlot => this.variablePlot;

        public TabControl PtabWidget => this.tabWidget;

        public Timer Ptimer => this.timer;

        public TextBox PobservedFileEdit => this.observedFileEdit;

        public ComboBox PstartDay => this.startDay;

        public ComboBox PfinishDay => this.finishDay;

        public ComboBox PstartMonth => this.startMonth;

        public ComboBox PfinishMonth => this.finishMonth;

        public TextBox PstartYear => this.startYear;

        public TextBox PfinishYear => this.finishYear;

        public DataGridView PtableWidget => this.tableWidget;

        public ComboBox PplotType => this.plotType;

        public TextBox PresultsFileEdit => this.resultsFileEdit;

        public ComboBox PvariableCombo => this.variableCombo;

        public ComboBox PanimationVariableCombo => this.animationVariableCombo;

        public ListBox PvariableList => this.variableList;

        public ComboBox PsummaryCombo => this.summaryCombo;

        public ComboBox PhruPlot => this.hruPlot;

        public ComboBox PsubPlot => this.subPlot;

        public RadioButton PcurrentAnimation => this.currentAnimation;

        public RadioButton PprintAnimation => this.printAnimation;

        public Label PdateLabel=> this.dateLabel;

        public RadioButton PcanvasAnimation => this.canvasAnimation;

        public RadioButton PlandscapeButton => this.landscapeButton;

        public NumericUpDown PprintCount => this.printCount;

        public GroupBox PcomposeOptions => this.composeOptions;

        public NumericUpDown PcomposeCount => this.composeCount;

        public TrackBar Pslider => this.slider;

        public NumericUpDown PspinBox => this.spinBox;

        public Button PrecordButton => this.recordButton;

        public Button PplayButton => this.playButton;

        public Label PcompareLabel => this.compareLabel;

        public Label PrecordLabel => this.recordLabel;

        private void scenariosCombo_SelectionChangeCommitted(object sender, EventArgs e) {
            this._parent.setupDb();
        }

        private void outputCombo_SelectedChangeCommitted(object sender, EventArgs e) {
            this._parent.setVariables();
        }

        private void PsummaryCombo_SelectionChangeCommitted(object sender, EventArgs e) {
            this._parent.changeSummary();
        }

        private void addButton_Click(object sender, EventArgs e) {
            this._parent.addClick();
        }

        private void allButton_Click(object sender, EventArgs e) {
            this._parent.allClick();
        }

        private void delButton_Click(object sender, EventArgs e) {
            this._parent.delClick();
        }

        private void clearbutton_Click(object sender, EventArgs e) {
            this._parent.clearClick();
        }

        private void resultsFileButton_Click(object sender, EventArgs e) {
            this._parent.setResultsFile();
        }

        private void tabWidget_SelectedIndexChanged(object sender, EventArgs e) {
            this._parent.modeChange();
        }

        private void saveButton_Click(object sender, EventArgs e) {
            this._parent.makeResults();
        }

        private void printButton_Click(object sender, EventArgs e) {
            this._parent.printResults();
        }

        private void canvasAnimation_CheckedChanged(object sender, EventArgs e) {
            this._parent.changeAnimationMode();
        }

        private void printAnimation_CheckedChanged(object sender, EventArgs e) {
            this._parent.changeAnimationMode();
        }

        private void animationVariableCombo_SelectionChangeCommitted(object sender, EventArgs e) {
            this._parent.setupAnimateLayer();
        }

        private void trackBar1_Scroll(object sender, EventArgs e) {
            this._parent.changeAnimate();
        }

        private void trackBar1_MouseDown(object sender, MouseEventArgs e) {
            this._parent.pressSlider();
        }

        private void playCommand_Click(object sender, EventArgs e) {
            this._parent.doPlay();
        }

        private void pauseCommand_Click(object sender, EventArgs e) {
            this._parent.doPause();
        }

        private void rewindCommand_Click(object sender, EventArgs e) {
            this._parent.doRewind();
        }

        private void recordButton_Click(object sender, EventArgs e) {
            this._parent.record();
        }

        private void playButton_Click(object sender, EventArgs e) {
            this._parent.playRecording();
        }

        private void spinBox_ValueChanged(object sender, EventArgs e) {
            this._parent.changeSpeed((int)spinBox.Value);
        }

        private void timer1_Tick(object sender, EventArgs e) {
            this._parent.doStep();
        }

        private void subPlot_SelectionChangeCommitted(object sender, EventArgs e) {
            this._parent.plotSetSub();
        }

        private void hruPlot_SelectionChangeCommitted(object sender, EventArgs e) {
            this._parent.plotSetHRU();
        }

        private void variablePlot_SelectionChangeCommitted(object sender, EventArgs e) {
            this._parent.plotSetVar();
        }

        private void addPlot_Click(object sender, EventArgs e) {
            this._parent.doAddPlot();
        }

        private void deletePlot_Click(object sender, EventArgs e) {
            this._parent.doDelPlot();
        }

        private void copyPlot_Click(object sender, EventArgs e) {
            this._parent.doCopyPlot();
        }

        private void upPlot_Click(object sender, EventArgs e) {
            this._parent.doUpPlot();
        }

        private void downPlot_Click(object sender, EventArgs e) {
            this._parent.doDownPlot();
        }

        private void addObserved_Click(object sender, EventArgs e) {
            this._parent.addObservedPlot();
        }

        private void observedFileButton_Click(object sender, EventArgs e) {
            this._parent.setObservedFile(observedFileEdit.Text);
        }

        private void plotButton_Click(object sender, EventArgs e) {
            this._parent.writePlotData();
        }

        private void closeButton_Click(object sender, EventArgs e) {
            this._parent.doClose();
        }

        private void compareButton_Click(object sender, EventArgs e) {
            this._parent.startCompareScenarios();
        }

        private void VisualiseForm_KeyUp(object sender, KeyEventArgs e) {
            if (e.KeyValue == 39) { // right arrow
                this._parent.animateStepRight();
            } else if (e.KeyValue == 37) {  // left arrow
                this._parent.animateStepLeft();
            }
        }

        private void slider_ValueChanged(object sender, EventArgs e) {
            this._parent.changeAnimate();
        }
    }
}
