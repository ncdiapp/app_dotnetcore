import React, { useCallback, useEffect, useState } from 'react';
import { useDispatch } from 'react-redux';
import * as wjInput from '@mescius/wijmo.react.input';



const TestDDL: React.FC = () => {
  const dispatch = useDispatch();

  

  const [items, setItems] = useState<any | null>(null);
  const [selectedValue, setSelectedValue] = useState<string | null>(null);


  const loadDataFromServer = useCallback(async () => {
    try {
      await new Promise(resolve => setTimeout(resolve, 300));

      const dataFromServer = ['a', 'b', 'c', 'd', 'e'];


     
      
      
      setItems(dataFromServer);
      setSelectedValue('');

      setTimeout(() => {
        
        setSelectedValue(null);

      }, 0);

     



    } catch (error) {
     
    } finally {
      
    }
  }, [dispatch]);

  useEffect(() => {

    loadDataFromServer();
  }, [loadDataFromServer]);

  const handleRefresh = () => {
    loadDataFromServer();
  };


  return (
    <div className={`flex h-full flex-col`}>
      <div className={`flex items-center justify-between px-4 py-2`}>
        <div className="text-sm font-semibold tracking-wide">
          ComboBox Simple Test
        </div>
        <div className="flex items-center gap-2">
          <button
            type="button"
            className={`rounded-full px-3 py-1 text-xs font-medium`}
            onClick={handleRefresh}
          >
            Refresh
          </button>


        </div>
      </div>

      <div className="h-1 flex-auto overflow-y-auto p-6">
        <div className="max-w-md">
          <label className="block text-sm font-medium mb-2">
            Select an option:
          </label>


          <wjInput.ComboBox
            itemsSource={items}
            selectedValue={selectedValue}
            selectedIndexChanged={(sender: any) => {
              setSelectedValue(sender.selectedValue);
            }}
            isRequired={false}
            className={`border w-full`}
          />

          <div className="mt-4 space-x-2">
            <button
              className={`px-4 py-2 rounded`}
              onClick={() => setSelectedValue('a')}
              disabled={!items || items.itemCount === 0}
            >
              Set A
            </button>

            <button
              className={`px-4 py-2 rounded`}
              onClick={() => setSelectedValue('b')}
              disabled={!items || items.itemCount === 0}
            >
              Set B
            </button>

            <button
              className={`px-4 py-2 rounded`}
              onClick={() => setSelectedValue('c')}
              disabled={!items || items.itemCount === 0}
            >
              Set C
            </button>

            <button
              className={`px-4 py-2 rounded`}
              onClick={() => setSelectedValue(null)}
            >
              Set Null
            </button>
          </div>

          <div className={`mt-4 p-4 border rounded`}>
            <p className="text-sm mb-2">
              <strong>Selected Value:</strong> {selectedValue === null ? '(null)' : selectedValue === '' ? '(empty)' : selectedValue}
            </p>
          </div>


        </div>
      </div>
    </div>
  );
};

export default TestDDL;