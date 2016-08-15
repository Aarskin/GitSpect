using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitSpect.Cmd
{
    public class GitObjectGraph : IEnumerable<GitObject>
    {
        private Dictionary<string, GitObject> _underlyingDictionary;        

        public GitObjectGraph()
        {
            _underlyingDictionary = new Dictionary<string, GitObject>();
        }

        internal void Store(GitObject gitObj)
        {
            _underlyingDictionary.Add(gitObj.SHA, gitObj);
        }

        /// <summary>
        /// Looks up an object, returning true and populating the out variable if it exists.
        /// Returns false and a null object if it does not exist
        /// </summary>
        /// <param name="objSha"></param>
        /// <param name="found"></param>
        /// <returns></returns>
        public bool LookupObject(string objSha, out GitObject found)
        {
            bool retVal = false;
            found = null;

            if (_underlyingDictionary.ContainsKey(objSha))
            {
                retVal = true;
                found = _underlyingDictionary[objSha];
            }

            return retVal;
        }

        public IEnumerator<GitObject> GetEnumerator()
        {
            return _underlyingDictionary.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _underlyingDictionary.Values.GetEnumerator();
        }
    }
}
