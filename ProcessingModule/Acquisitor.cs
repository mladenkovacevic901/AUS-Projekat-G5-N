using Common;
using System;
using System.Threading;

namespace ProcessingModule
{
    //test1
    /// <summary>
    /// Class containing logic for periodic polling.
    /// </summary>
    public class Acquisitor : IDisposable
	{
		private AutoResetEvent acquisitionTrigger;
        private IProcessingManager processingManager;
        private Thread acquisitionWorker;
		private IStateUpdater stateUpdater;
		private IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Acquisitor"/> class.
        /// </summary>
        /// <param name="acquisitionTrigger">The acquisition trigger.</param>
        /// <param name="processingManager">The processing manager.</param>
        /// <param name="stateUpdater">The state updater.</param>
        /// <param name="configuration">The configuration.</param>
		public Acquisitor(AutoResetEvent acquisitionTrigger, IProcessingManager processingManager, IStateUpdater stateUpdater, IConfiguration configuration)
		{
			this.stateUpdater = stateUpdater;
			this.acquisitionTrigger = acquisitionTrigger;
			this.processingManager = processingManager;
			this.configuration = configuration;
			this.InitializeAcquisitionThread();
			this.StartAcquisitionThread();
		}

		#region Private Methods

        /// <summary>
        /// Initializes the acquisition thread.
        /// </summary>
		private void InitializeAcquisitionThread()
		{
			this.acquisitionWorker = new Thread(Acquisition_DoWork);
			this.acquisitionWorker.Name = "Acquisition thread";
		}

        /// <summary>
        /// Starts the acquisition thread.
        /// </summary>
		private void StartAcquisitionThread()
		{
			acquisitionWorker.Start();
		}

        /// <summary>
        /// Acquisitor thread logic.
        /// </summary>
		private void Acquisition_DoWork()
		{
            //TO DO: IMPLEMENT
            while (true)
            {
                try
                {
                    // Cekaj signal tajmera (okida se svake sekunde)
                    acquisitionTrigger.WaitOne();

                    // Prodi kroz sve konfigurisane registre iz RtuCfg.txt
                    foreach (var configItem in configuration.GetConfigurationItems())
                    {
                        // Povecaj brojac sekundi za ovaj registar
                        configItem.SecondsPassedSinceLastPoll++;

                        // Da li je proslo dovoljno sekundi?
                        if (configItem.SecondsPassedSinceLastPoll >= configItem.AcquisitionInterval)
                        {
                            // Resetuj brojac
                            configItem.SecondsPassedSinceLastPoll = 0;

                            // Posalji READ komandu za ovaj blok registara
                            processingManager.ExecuteReadCommand(
                                configItem,                          // koji registar
                                configuration.GetTransactionId(),   // ID transakcije
                                configuration.UnitAddress,          // adresa uredjaja (148)
                                configItem.StartAddress,             // od koje adrese
                                configItem.NumberOfRegisters         // koliko registara
                            );
                        }
                    }
                }
                catch (Exception ex)
                {
                    stateUpdater.LogMessage(ex.Message);
                }
            }
        }

        #endregion Private Methods

        /// <inheritdoc />
        public void Dispose()
		{
			acquisitionWorker.Abort();
        }
	}
}