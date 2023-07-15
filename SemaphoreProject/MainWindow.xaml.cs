using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace SemaphoreProject
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<Thread> Threads { get; set; }
        public SemaphoreSlim semaphore { get; set; }
        public int Count { get; set; } = 0;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Threads = new ObservableCollection<Thread>();
            Count = 4;
            semaphore = new SemaphoreSlim(Count, Count);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(SomeMethod);
            thread.Name = $"Thread {Threads.Count + 1}";
            Threads.Add(thread);
            createdbox.Items.Add(thread);
            thread.Start();
        }

        public void SomeMethod()
        {
            if (semaphore == null)
                return;

            bool isFinish = false;
            bool b = false;

            while (!isFinish)
            {
                if (semaphore.Wait(2000))
                {
                    Thread Current = null;

                    Dispatcher.Invoke(() =>
                    {
                        Current = waitingbox.Items[0] as Thread;
                    });

                    if (Current == null)
                        return;

                    try
                    {
                        Thread.Sleep(200);

                        Dispatcher.Invoke(() =>
                        {
                            Workingbox.Items.Add($"{Current.Name} -> {Current.ManagedThreadId}");
                            waitingbox.Items.Remove(Current);
                        });

                        Thread.Sleep(6000);
                    }
                    catch (ThreadInterruptedException)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Workingbox.Items.Remove($"{Current.Name} -> {Current.ManagedThreadId}");
                            semaphore.Release();
                            b = true;
                        });
                    }
                    finally
                    {
                        if (!b)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                Workingbox.Items.Remove($"{Current.Name} -> {Current.ManagedThreadId}");
                                semaphore.Release();
                            });
                        }
                        Threads.Remove(Current);
                        isFinish = true;
                    }
                }
            }
        }

        private void createdbox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var l = sender as ListBox;
            var t = l?.SelectedItem as Thread;
            if (t == null) return;
            waitingbox.Items.Add(t);
            t.Start();
            if (l != null) l.Items.Remove(t);
        }

        private void Workingbox_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var selectedThread = Workingbox.SelectedItem as string;
            var threadName = selectedThread?.Split("->")[0].Trim();
            var thread = Threads.FirstOrDefault(t => t.Name == threadName);
            if (thread == null) return;
            if (thread.ThreadState == ThreadState.WaitSleepJoin) thread.Interrupt();
            Threads.Remove(thread);
        }

        private async void DecreaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Count > 0)
            {
                Count--;
                semaphore.Release();
            }

            await Task.Delay(1); // Asenkron bir bekleme işlemi gerçekleştirin
        }

        private async void IncreaseButton_Click(object sender, RoutedEventArgs e)
        {
            if (Count < 10)
            {
                Count++;
                semaphore.Release();

                await Task.Delay(1); // Asenkron bir bekleme işlemi gerçekleştirin

                Thread thread = new Thread(() =>
                {
                    semaphore.Wait();
                    Dispatcher.Invoke(() => { }); // Boş bir işlem yaparak thread'in devam etmesini sağlayın
                });
                thread.Start();
            }
        }
    }
}