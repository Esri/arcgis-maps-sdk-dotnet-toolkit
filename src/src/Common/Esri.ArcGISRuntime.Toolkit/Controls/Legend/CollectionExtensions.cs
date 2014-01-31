// (c) Copyright ESRI.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Esri.ArcGISRuntime.Toolkit.Controls
{
	/// <summary>
	/// IEnumerable extensions : ToObservableCollection, ForEach, Descendants, Leaves
	/// </summary>
	internal static class CollectionExtensions
	{
		/// <summary>
		/// Converts an enumerable to an observable collection.
		/// </summary>
		/// <typeparam name="T">The type of objects in the enumeration.</typeparam>
		/// <param name="enumerable">The enumerable.</param>
		/// <returns></returns>
		public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> enumerable)
		{
			var observableCollection = new ObservableCollection<T>();

			foreach (T item in enumerable)
				observableCollection.Add(item);

			return observableCollection;
		}


		/// <summary>
		///  Performs the specified action on each element of the enumeration.
		/// </summary>
		/// <typeparam name="T">The type of objects in the enumeration.</typeparam>
		/// <param name="enumerable">The enumerable.</param>
		/// <param name="action">The System.Action delegate to perform on each element of the enumeration.</param>
		public static void ForEach<T>(this IEnumerable<T> enumerable, Action<T> action)
		{
			if (enumerable != null)
				foreach (T item in enumerable)
					action(item);
		}

		/// <summary>
		/// Find all descendants from an enumerable.
		/// </summary>
		/// <typeparam name="T">The type of objects in the enumeration.</typeparam>
		/// <param name="enumerable">The enumerable.</param>
		/// <param name="childrenDelegate">The children delegate.</param>
		/// <returns></returns>
		public static IEnumerable<T> Descendants<T>(this IEnumerable<T> enumerable, Func<T, IEnumerable<T>> childrenDelegate)
		{
			if (enumerable == null)
				yield break;

			foreach (T item in enumerable)
			{
				yield return item;

				IEnumerable<T> children = childrenDelegate(item);
				if (children != null)
				{
					foreach (T child in children.Descendants(childrenDelegate))
						yield return child;
				}
			}
		}

		/// <summary>
		/// Find all leaves from an enumerable.
		/// </summary>
		/// <typeparam name="T">The type of objects in the enumeration.</typeparam>
		/// <param name="enumerable">The enumerable.</param>
		/// <param name="childrenDelegate">The children delegate.</param>
		/// <returns></returns>
		public static IEnumerable<T> Leaves<T>(this IEnumerable<T> enumerable, Func<T, IEnumerable<T>> childrenDelegate)
		{
			if (enumerable == null)
				yield break;

			foreach (T item in enumerable)
			{

				IEnumerable<T> children = childrenDelegate(item);
				if (children == null)
				{
					// it's a leaf
					yield return item;
				}
				else
				{
					// not a leaf ==> recursive call
					foreach (T child in children.Leaves(childrenDelegate))
						yield return child;
				}
			}
		}
	}
}
